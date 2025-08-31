package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.domain.PartyInvitation;
import com.ggumtle.ggumtle.dream.persistence.PartyInvitationRepository;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
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
    private final PartyParticipantRepository partyParticipantRepository;
    private final PartyInvitationRepository partyInvitationRepository;

    @SocketCommandHandler(command = SocketCommand.CREATE_PARTY)
    public void createParty(String sessionId, CreatePartyCommand command) {
        if (partyParticipantRepository.existsById(command.memberId())) {
            throw new RuntimeException("이미 방에 속해 있습니다");
        }

        PartyParticipant partyParticipant = new PartyParticipant(command.memberId(), UUID.randomUUID().toString(), true);
        partyParticipantRepository.save(partyParticipant);

        SendSocketEvent event = new SendSocketEvent(List.of(sessionId), new CreatePartyResult(partyParticipant.getPartyId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(command = SocketCommand.INVITE_PARTY)
    public void inviteParty(String sessionId, InvitePartyCommand command) {
        PartyParticipant partyParticipant = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new RuntimeException("속한 방이 없습니다"));

        PartyInvitation partyInvitation = PartyInvitation.builder()
                .id(UUID.randomUUID().toString())
                .partyId(partyParticipant.getPartyId())
                .inviteeId(command.inviteeId())
                .inviterId(command.requesterId())
                .build();
        partyInvitationRepository.save(partyInvitation);

        // TODO: WebSocket에 Principal 주입 후, 초대 한 사용자와 받은 사용자의 Long ID들을 반환하도록 수정
        SendSocketEvent event = new SendSocketEvent(List.of(sessionId), new InvitePartyResult(partyInvitation.getId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(command = SocketCommand.ACCEPT_PARTY_INVITATION)
    public void acceptPartyInvitation(String sessionId, AcceptPartyInvitationCommand command) {
        PartyInvitation partyInvitation = partyInvitationRepository.findById(command.invitationId())
                .orElseThrow(() -> new RuntimeException("존재하거나 이미 처리된 초대입니다"));

        if (!partyInvitation.getInviteeId().equals(command.requesterId())) {
            throw new RuntimeException("본인이 받은 파티 초대만 수락할 수 있습니다");
        }

        partyInvitationRepository.delete(partyInvitation);
        PartyParticipant partyParticipant = new PartyParticipant(partyInvitation.getInviteeId(), partyInvitation.getPartyId(), false);
        partyParticipantRepository.save(partyParticipant);

        // TODO: WebSocket에 Principal 주입 후, 해당 파티에 속한 모든 사용자의 Long ID들을 반환하도록 수정
        // TODO: Member 도메인 기능 구현 후, 가입한 사용자의 닉네임 조회 후 반환하도록 수정
        SendSocketEvent event = new SendSocketEvent(List.of(sessionId), new AcceptInvitationResult(partyParticipant.getMemberId(), "Temp nickname"));
        applicationEventPublisher.publishEvent(event);
    }
}
