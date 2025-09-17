package com.ggumtle.ggumtle.presentation;

import com.ggumtle.ggumtle.common.DisconnectedEventListener;
import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.common.event.UserDisconnectedEvent;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.presentation.response.LeavePartyResponse;
import com.ggumtle.ggumtle.friend.application.MemberStateService;
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
    private final MemberStateService memberStateService;
    private final DreamPartyService dreamPartyService;
    private final DisconnectedEventListener disconnectedEventListener;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) {
        socketResponseDispatcher.registerSession(session);

        Long memberId = Long.parseLong(session.getPrincipal().getName());

        memberStateService.setOnline(memberId);

        log.info("WS CONNECT: memberId = {}", memberId);
        SendSocketEvent sendSocketEvent = new SendSocketEvent(SocketType.CONNECT, List.of(memberId), "API 서버와 연결 완료");
        applicationEventPublisher.publishEvent(sendSocketEvent);
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) {
        Long memberId = Long.parseLong(session.getPrincipal().getName());

        applicationEventPublisher.publishEvent(new UserDisconnectedEvent(memberId));

        memberStateService.setOffline(memberId);
        socketResponseDispatcher.removeSession(session);
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) {
        socketRequestDispatcher.dispatchSocketResponse(session, message);
    }
}
