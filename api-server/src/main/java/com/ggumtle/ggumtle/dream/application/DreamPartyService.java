package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import com.ggumtle.ggumtle.common.SocketCommand;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import java.util.List;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamPartyService {
    private final ApplicationEventPublisher applicationEventPublisher;

    @SocketCommandHandler(command = SocketCommand.CREATE_PARTY)
    public void createParty(String sessionId, CreatePartyCommand command) {
        SendSocketEvent event = new SendSocketEvent(List.of(sessionId), "파티가 생성되었습니다");
        applicationEventPublisher.publishEvent(event);
    }
}
