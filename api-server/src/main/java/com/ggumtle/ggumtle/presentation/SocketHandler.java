package com.ggumtle.ggumtle.presentation;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class SocketHandler extends TextWebSocketHandler {
    private final SocketRequestDispatcher socketRequestDispatcher;
    private final SocketResponseDispatcher socketResponseDispatcher;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        socketResponseDispatcher.registerSession(session);
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
