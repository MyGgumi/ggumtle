package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.domain.Party;
import com.ggumtle.ggumtle.dream.persistence.PartyRepository;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import com.ggumtle.ggumtle.common.SocketCommand;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.UUID;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamPartyService {
    private final ApplicationEventPublisher applicationEventPublisher;
    private final PartyRepository partyRepository;

    @SocketCommandHandler(command = SocketCommand.CREATE_PARTY)
    public void createParty(String sessionId, CreatePartyCommand command) {
        if (partyRepository.existsById(command.memberId())) {
            throw new RuntimeException("이미 방에 속해 있습니다");
        }

        Party party = new Party(command.memberId(), UUID.randomUUID().toString(), true);
        partyRepository.save(party);

        SendSocketEvent event = new SendSocketEvent(List.of(sessionId), new CreatePartyResult(party.getPartyId()));
        applicationEventPublisher.publishEvent(event);
    }
}
