package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;

import java.util.Collections;
import java.util.HashSet;
import java.util.Set;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class SocketResponseDispatcher {
    private static final Set<WebSocketSession> sessions = Collections.synchronizedSet(new HashSet<>());
    private final ObjectMapper objectMapper;

    void registerSession(WebSocketSession session) {
        sessions.add(session);
        sendMessage(session, new SocketResponse(true, "API 서버와 연결 완료"));
    }

    void removeSession(WebSocketSession session) {
        sessions.remove(session);
    }

    @EventListener
    private void handleSocketMessageEvent(SendSocketEvent event) {
        SocketResponse response = new SocketResponse(true, event.data());

        for (String sessionId : event.sessionIds()) {

            for (WebSocketSession session : sessions) {
                if (session.getId().equals(sessionId)) {
                    sendMessage(session, response);
                    break;
                }
            }

        }
    }

    private void sendMessage(WebSocketSession session, SocketResponse response) {
        try {
            String json = objectMapper.writeValueAsString(response);

            session.sendMessage(new TextMessage(json));
        } catch (Exception e) {
            // TODO: 메시지 파싱 예외 처리
            e.printStackTrace();
        }
    }

    private record SocketResponse (
            Boolean success,
            Object data
    ) {
    }
}
