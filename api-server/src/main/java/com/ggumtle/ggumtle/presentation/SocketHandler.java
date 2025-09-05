package com.ggumtle.ggumtle.presentation;

import com.ggumtle.ggumtle.common.SocketType;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;

import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketHandler extends TextWebSocketHandler {
    private final SocketRequestDispatcher socketRequestDispatcher;
    private final SocketResponseDispatcher socketResponseDispatcher;
    private final ApplicationEventPublisher applicationEventPublisher;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        socketResponseDispatcher.registerSession(session);

        Long memberId = Long.parseLong(session.getPrincipal().getName());

        log.info("WS CONNECT: memberId = {}", memberId);
        SendSocketEvent sendSocketEvent = new SendSocketEvent(SocketType.CONNECT, List.of(memberId), "API 서버와 연결 완료");
        applicationEventPublisher.publishEvent(sendSocketEvent);
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        socketResponseDispatcher.removeSession(session);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) {
        socketRequestDispatcher.dispatchSocketResponse(session, message);
    }
}
