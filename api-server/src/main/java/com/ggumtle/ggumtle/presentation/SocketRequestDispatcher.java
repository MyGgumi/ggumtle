package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.SocketCommand;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationContext;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.ApplicationListener;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;

import java.lang.reflect.Method;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketRequestDispatcher implements ApplicationListener<ContextRefreshedEvent> {
    private final Map<SocketCommand, HandlerInfo> commandToHandler = new HashMap<>();
    private final ObjectMapper objectMapper;
    private final ApplicationContext applicationContext;
    private final ApplicationEventPublisher applicationEventPublisher;

    @Override
    public void onApplicationEvent(ContextRefreshedEvent event) {
        initializeSocketCommandHandler();
    }

    protected void initializeSocketCommandHandler() {
        for (String beanName : applicationContext.getBeanDefinitionNames()) {
            Object bean = applicationContext.getBean(beanName);

            for (Method method : bean.getClass().getDeclaredMethods()) {
                SocketCommandHandler annotation = method.getAnnotation(SocketCommandHandler.class);
                if (annotation == null) {
                    continue;
                }

                SocketCommand command = annotation.command();
                Class<?>[] paramTypes = method.getParameterTypes();

                if (commandToHandler.containsKey(command)) {
                    throw new IllegalStateException("중복된 커맨드 핸들러: " + command);
                }

                if (paramTypes.length == 0) {
                    commandToHandler.put(command, new HandlerInfo(bean, method, null, false));
                    continue;
                }

                if (paramTypes.length == 1) {
                    commandToHandler.put(command, new HandlerInfo(bean, method, paramTypes[0], false));
                    continue;
                }

                if (paramTypes.length == 2) {
                    if (paramTypes[0] != String.class) {
                        throw new RuntimeException("파라미터가 2개인 소켓 커맨드 핸들러의 첫번째 파라미터는 세션 ID여야 합니다");
                    }
                    commandToHandler.put(command, new HandlerInfo(bean, method, paramTypes[1], true));
                    continue;
                }

                throw new RuntimeException("소켓 커맨드 핸들러의 파라미터는 2개 이하여야 합니다");
            }
        }
    }

    void dispatchSocketResponse(WebSocketSession session, TextMessage message) {
        log.info("Received Message: {}", message);

        // TODO: 파싱 예외 처리
        SocketRequest socketRequest;
        try {
            socketRequest = objectMapper.readValue(message.getPayload(), SocketRequest.class);
            log.info("Parsed Message: {}", socketRequest);
        } catch (Exception e) {
            e.printStackTrace();
            return;
        }

        SocketCommand socketCommand = SocketCommand.valueOf(socketRequest.command);
        log.info("커맨드 {}", socketCommand);
        HandlerInfo handlerInfo = commandToHandler.get(socketCommand);
        log.info("핸들러 {}", handlerInfo);
        Object parameter = objectMapper.convertValue(socketRequest.data(), handlerInfo.parameterType());
        log.info("파라미터 {}", parameter);

        try {
            if (handlerInfo.needSessionId) {
                handlerInfo.method().invoke(handlerInfo.bean(), session.getId(), parameter);
            } else {
                handlerInfo.method().invoke(handlerInfo.bean(), parameter);
            }
        } catch (Exception e) {
            if (e.getCause() instanceof RuntimeException runtimeException) {
                log.error("Runtime exception 발생: {}", runtimeException.getMessage());

                SendErrorSocketEvent event = new SendErrorSocketEvent(List.of(session.getId()), runtimeException.getMessage());
                applicationEventPublisher.publishEvent(event);

                return;
            }

            log.error("Exception 발생: {}", e.getMessage());
        }
    }

    private record SocketRequest(
            String command,
            Object data
    ) {
    }

    private record HandlerInfo(
            Object bean,
            Method method,
            Class<?> parameterType,
            Boolean needSessionId
    ) {
    }
}
