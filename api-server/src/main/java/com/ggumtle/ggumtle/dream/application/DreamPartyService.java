package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.domain.PartyInvitation;
import com.ggumtle.ggumtle.dream.persistence.PartyInvitationRepository;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamPartyService {
    private final ApplicationEventPublisher applicationEventPublisher;
    private final PartyParticipantRepository partyParticipantRepository;
    private final PartyInvitationRepository partyInvitationRepository;

    public CreatePartyResult createParty(CreatePartyCommand command) {
        if (partyParticipantRepository.existsById(command.memberId())) {
            throw new RuntimeException("이미 방에 속해 있습니다");
        }

        PartyParticipant partyParticipant = new PartyParticipant(command.memberId(), UUID.randomUUID().toString(), true);
        partyParticipantRepository.save(partyParticipant);

        return new CreatePartyResult(List.of(partyParticipant.getMemberId()), partyParticipant.getPartyId());
    }

    public InvitePartyResult inviteParty(InvitePartyCommand command) {
        PartyParticipant inviter = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new RuntimeException("속한 방이 없습니다"));

        PartyInvitation partyInvitation = PartyInvitation.builder()
                .id(UUID.randomUUID().toString())
                .partyId(inviter.getPartyId())
                .inviteeId(command.inviteeId())
                .inviterId(command.requesterId())
                .build();
        partyInvitationRepository.save(partyInvitation);

        List<Long> memberIds = new ArrayList<>();
        memberIds.add(partyInvitation.getInviteeId());
        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(inviter.getPartyId());
        participants.stream().forEach(participant -> memberIds.add(participant.getMemberId()));

        return new InvitePartyResult(memberIds, partyInvitation.getId());
    }

    public AcceptPartyInvitationResult acceptPartyInvitation(AcceptPartyInvitationCommand command) {
        PartyInvitation partyInvitation = partyInvitationRepository.findById(command.invitationId())
                .orElseThrow(() -> new RuntimeException("존재하거나 이미 처리된 초대입니다"));

        if (!partyInvitation.getInviteeId().equals(command.requesterId())) {
            throw new RuntimeException("본인이 받은 파티 초대만 수락할 수 있습니다");
        }

        partyInvitationRepository.delete(partyInvitation);
        PartyParticipant joinedParticipant = new PartyParticipant(partyInvitation.getInviteeId(), partyInvitation.getPartyId(), false);
        partyParticipantRepository.save(joinedParticipant);

        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(joinedParticipant.getPartyId());
        List<Long> participantIds = participants.stream().map(PartyParticipant::getMemberId).toList();

        // TODO: Member 도메인 기능 구현 후, 가입한 사용자의 닉네임 조회 후 반환하도록 수정
        return new AcceptPartyInvitationResult(participantIds, joinedParticipant.getMemberId(), "Temp nickname");
    }
}
