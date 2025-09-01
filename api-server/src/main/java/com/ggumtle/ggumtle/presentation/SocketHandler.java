package com.ggumtle.ggumtle.presentation;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketHandler extends TextWebSocketHandler {
    private final SocketRequestDispatcher socketRequestDispatcher;
    private final SocketResponseDispatcher socketResponseDispatcher;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        socketResponseDispatcher.registerSession(session);

        String memberId = resolveMemberId(session);
        log.info("WS CONNECT: memberId = {}", memberId);
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        socketResponseDispatcher.removeSession(session);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) {
        socketRequestDispatcher.dispatchSocketResponse(session, message);
    }

    private String resolveMemberId(WebSocketSession session) {
        if (session.getPrincipal() != null) {
            return session.getPrincipal().getName();
        }
        Object attr = session.getAttributes().get("memberId");
        return (attr != null) ? String.valueOf(attr) : "null";
    }
}
