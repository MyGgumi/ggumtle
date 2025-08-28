package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.SocketCommand;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationContext;
import org.springframework.context.ApplicationListener;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;

import java.lang.reflect.Method;
import java.util.HashMap;
import java.util.Map;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketRequestDispatcher implements ApplicationListener<ContextRefreshedEvent> {
    private final Map<SocketCommand, HandlerInfo> commandToHandler = new HashMap<>();
    private final ObjectMapper objectMapper;
    private final ApplicationContext applicationContext;

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

                commandToHandler.put(command, new HandlerInfo(bean, method, paramTypes[0]));
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
            handlerInfo.method().invoke(handlerInfo.bean(), session.getId(), parameter);
        } catch (Exception e) {
            // TODO: 기능 수행 중 발생한 예외 처리
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
            Class<?> parameterType
    ) {
    }
}
