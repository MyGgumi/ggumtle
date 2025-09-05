package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.CommonErrorCode;
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
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketRequestDispatcher implements ApplicationListener<ContextRefreshedEvent> {
    private final Map<SocketType, HandlerInfo> typeToHandler = new HashMap<>();
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

                SocketType type = annotation.type();
                Class<?>[] paramTypes = method.getParameterTypes();

                if (typeToHandler.containsKey(type)) {
                    log.warn("{} 핸들러 무시: 중복된 핸들러", type);
                    continue;
                }

                typeToHandler.put(type, new HandlerInfo(bean, method, paramTypes));
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

        // 요청 유형 파싱
        SocketType type;
        try {
            type = SocketType.valueOfRequest(socketRequest.type);
            log.info("요청 유형: {}", type);
        } catch (GgumtleException e) {
            log.error("소켓 유형 파싱에 실패했습니다");

            SendErrorSocketEvent event = new SendErrorSocketEvent(
                    SocketType.UNKNOWN,
                    List.of(Long.parseLong(session.getPrincipal().getName())),
                    e.getCode(),
                    e.getMessage());
            applicationEventPublisher.publishEvent(event);

            return;
        }

        // 핸들러 정보 조회 및 파라미터 파싱
        HandlerInfo handlerInfo;
        Object[] parameters;
        try {
            handlerInfo = typeToHandler.get(type);
            log.info("요청 핸들러: {}", handlerInfo);
            parameters = new Object[handlerInfo.parameterTypes.length];
            for (int i = 0; i < handlerInfo.parameterTypes.length; i++) {
                if (handlerInfo.parameterTypes[i].equals(WebSocketSession.class)) {
                    parameters[i] = session;
                    continue;
                }

                try {
                    parameters[i] = objectMapper.convertValue(socketRequest.data(), handlerInfo.parameterTypes[i]);
                } catch (Exception e) {
                    String errorMessage = String.format("요청 데이터 [%s]을 %s형으로 매핑할 수 없습니다", socketRequest.data(), handlerInfo.parameterTypes[i]);
                    log.error(errorMessage);

                    SendErrorSocketEvent event = new SendErrorSocketEvent(
                            type,
                            List.of(Long.parseLong(session.getPrincipal().getName())),
                            CommonErrorCode.BAD_REQUEST.getCode(),
                            CommonErrorCode.BAD_REQUEST.getMessage());
                    applicationEventPublisher.publishEvent(event);

                    return;
                }
            }
            log.info("요청 파라미터: {}", Arrays.toString(parameters));

        } catch (RuntimeException e) {
            return;
        }

        // 핸들러 호출
        try {
            handlerInfo.method().invoke(handlerInfo.bean(), parameters);
        } catch (Exception e) {
            if (e.getCause() instanceof GgumtleException ggumtleException) {
                log.error("Ggumtle exception 발생: {}", ggumtleException.getMessage());

                SendErrorSocketEvent event = new SendErrorSocketEvent(
                        type,
                        List.of(Long.parseLong(session.getPrincipal().getName())),
                        ggumtleException.getCode(),
                        ggumtleException.getMessage());
                applicationEventPublisher.publishEvent(event);

                return;
            }

            e.printStackTrace();
            log.error("Exception 발생: {}", e.getMessage());

            SendErrorSocketEvent event = new SendErrorSocketEvent(
                    type,
                    List.of(Long.parseLong(session.getPrincipal().getName())),
                    CommonErrorCode.INTERNAL_SERVER_ERROR.getCode(),
                    CommonErrorCode.INTERNAL_SERVER_ERROR.getMessage());
            applicationEventPublisher.publishEvent(event);
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
            Class<?>[] parameterTypes
    ) {
    }
}
