package com.ggumtle.ggumtle.presentation;

import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;

import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class SocketResponseDispatcher {
    private static final ConcurrentHashMap<Long, WebSocketSession> idToSession = new ConcurrentHashMap<>();
    private final ObjectMapper objectMapper;

    void registerSession(WebSocketSession session) {
        Long memberId = Long.parseLong(session.getPrincipal().getName());

        idToSession.put(memberId, session);
        sendMessage(session, new SocketResponse(true, null, "API 서버와 연결 완료"));
    }

    void removeSession(WebSocketSession session) {
        Long memberId = Long.parseLong(session.getPrincipal().getName());

        idToSession.remove(memberId);
    }

    @EventListener
    private void handleSocketMessageEvent(SendSocketEvent event) {
        SocketResponse response = new SocketResponse(true, null, event.data());

        for (Long memberId : event.memberIds()) {
            WebSocketSession session = idToSession.get(memberId);

            if (session == null) {
                log.warn("{}번 사용자의 세션이 없습니다", memberId);
                continue;
            }

            sendMessage(session, response);
        }
    }

    @EventListener
    private void handleSocketErrorMessageEvent(SendErrorSocketEvent event) {
        SocketResponse response = new SocketResponse(false, event.code(), event.message());

        for (Long memberId : event.memberIds()) {
            WebSocketSession session = idToSession.get(memberId);

            if (session == null) {
                log.warn("{}번 사용자의 세션이 없습니다", memberId);
                continue;
            }

            sendMessage(session, response);
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
            String code,
            Object data
    ) {
    }
}
