package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.common.SocketRequestType;
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
    private final Map<SocketRequestType, HandlerInfo> typeToHandler = new HashMap<>();
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

                SocketRequestType type = annotation.type();
                Class<?>[] paramTypes = method.getParameterTypes();

                if (typeToHandler.containsKey(type)) {
                    log.warn("{} 핸들러 무시: 중복된 핸들러", type);
                    continue;
                }

                if (paramTypes.length == 0) {
                    typeToHandler.put(type, new HandlerInfo(bean, method, null, false));
                    continue;
                }

                if (paramTypes.length == 1) {
                    if (paramTypes[0] == WebSocketSession.class) {
                        typeToHandler.put(type, new HandlerInfo(bean, method, null, true));
                        continue;
                    }
                    else {
                        typeToHandler.put(type, new HandlerInfo(bean, method, paramTypes[0], false));
                        continue;
                    }
                }

                if (paramTypes.length == 2) {
                    if (paramTypes[1] != WebSocketSession.class) {
                        log.warn("{} 핸들러 무시: 파라미터가 2개인 소켓 요청 핸들러의 첫번째 파라미터는 Request DTO, 두번째 파라미터는 WebSocketSession이어야 합니다", type);
                        continue;
                    }

                    typeToHandler.put(type, new HandlerInfo(bean, method, paramTypes[0], true));
                    continue;
                }

                log.warn("{} 핸들러 무시: 소켓 커맨드 핸들러의 파라미터는 2개 이하여야 합니다", type);
            }
        }

        log.info(typeToHandler.toString());
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

        SocketRequestType type = SocketRequestType.valueOf(socketRequest.type);
        log.info("요청 유형: {}", type);
        HandlerInfo handlerInfo = typeToHandler.get(type);
        log.info("요청 핸들러: {}", handlerInfo);
        Object parameter = null;
        if (handlerInfo.parameterType() != null) {
            parameter = objectMapper.convertValue(socketRequest.data(), handlerInfo.parameterType());
        }
        log.info("요청 파라미터: {}", parameter);

        try {
            if (handlerInfo.parameterType != null && handlerInfo.needSession) {
                handlerInfo.method().invoke(handlerInfo.bean(), parameter, session);
                return;
            }

            if (handlerInfo.needSession) {
                handlerInfo.method().invoke(handlerInfo.bean(), session);
                return;
            }

            if (handlerInfo.parameterType != null) {
                handlerInfo.method().invoke(handlerInfo.bean(), parameter);
                return;
            }

            handlerInfo.method().invoke(handlerInfo.bean());
        } catch (Exception e) {
            if (e.getCause() instanceof RuntimeException runtimeException) {
                log.error("Runtime exception 발생: {}", runtimeException.getMessage());

                SendErrorSocketEvent event = new SendErrorSocketEvent(List.of(Long.parseLong(session.getPrincipal().getName())), null, runtimeException.getMessage());
                applicationEventPublisher.publishEvent(event);

                return;
            }

            e.printStackTrace();
            log.error("Exception 발생: {}", e.getMessage());
        }
    }

    private record SocketRequest(
            String type,
            Object data
    ) {
    }

    private record HandlerInfo(
            Object bean,
            Method method,
            Class<?> parameterType,
            Boolean needSession
    ) {
    }
}
