package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.ReadyDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.application.result.ReadyDreamResult;
import com.ggumtle.ggumtle.dream.application.result.UnreadyDreamResult;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.domain.PartyInvitation;
import com.ggumtle.ggumtle.dream.domain.WaitingParty;
import com.ggumtle.ggumtle.dream.persistence.PartyInvitationRepository;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.DreamErrorCode;
import com.ggumtle.ggumtle.exception.code.MemberErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.member.persistence.MemberRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Objects;
import java.util.Set;
import java.util.UUID;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamPartyService {
    private static final String WAITING_PARTY_KEY = "waiting_party";

    private final PartyParticipantRepository partyParticipantRepository;
    private final PartyInvitationRepository partyInvitationRepository;
    private final MemberRepository memberRepository;
    private final RedisTemplate<String, WaitingParty> waitingPartyRedisTemplate;

    public CreatePartyResult createParty(CreatePartyCommand command) {
        if (partyParticipantRepository.existsById(command.memberId())) {
            throw new GgumtleException(DreamErrorCode.ALREADY_IN_PARTY);
        }

        PartyParticipant partyParticipant = new PartyParticipant(command.memberId(), UUID.randomUUID().toString(), true);
        partyParticipantRepository.save(partyParticipant);

        return new CreatePartyResult(List.of(partyParticipant.getMemberId()), partyParticipant.getPartyId());
    }

    public InvitePartyResult inviteParty(InvitePartyCommand command) {
        PartyParticipant inviter = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(
                        DreamErrorCode.NOT_FOUND_PARTY,
                        "현재 사용자가 속한 파티가 없어 다른 사용자를 파티에 초대할 수 없습니다"));

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
        participants.forEach(participant -> memberIds.add(participant.getMemberId()));

        return new InvitePartyResult(memberIds, partyInvitation.getId());
    }

    public AcceptPartyInvitationResult acceptPartyInvitation(AcceptPartyInvitationCommand command) {
        Member member = memberRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(MemberErrorCode.NOT_FOUND, command.requesterId() + "번 사용자를 찾을 수 없습니다"));

        if (partyParticipantRepository.existsById(command.requesterId())) {
            throw new GgumtleException(DreamErrorCode.ALREADY_IN_PARTY);
        }

        PartyInvitation partyInvitation = partyInvitationRepository.findById(command.invitationId())
                .orElseThrow(() -> new GgumtleException(
                        DreamErrorCode.NOT_FOUND_INVITATION,
                        "존재하지 않거나 이미 수락된 초대입니다"));

        if (!partyInvitation.getInviteeId().equals(command.requesterId())) {
            throw new GgumtleException(DreamErrorCode.NOT_MY_INVITATION);
        }

        partyInvitationRepository.delete(partyInvitation);
        PartyParticipant joinedParticipant = new PartyParticipant(partyInvitation.getInviteeId(), partyInvitation.getPartyId(), false);
        partyParticipantRepository.save(joinedParticipant);

        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(joinedParticipant.getPartyId());
        List<Long> participantIds = participants.stream().map(PartyParticipant::getMemberId).toList();

        return new AcceptPartyInvitationResult(participantIds, joinedParticipant.getMemberId(), member.getNickname());
    }

    public ReadyDreamResult readyDream(ReadyDreamCommand command) {
        PartyParticipant requester = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        if (!requester.isReady()){
            requester.ready();
            partyParticipantRepository.save(requester);
        }

        List<Long> participantMemberIds = partyParticipantRepository.findAllByPartyId(requester.getPartyId())
                .stream().map(PartyParticipant::getMemberId).toList();

        return new ReadyDreamResult(participantMemberIds, requester.getMemberId(), true);
    }

    public UnreadyDreamResult unreadyDream(ReadyDreamCommand command) {
        PartyParticipant requester = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        if (requester.isReady()){
            requester.unready();
            partyParticipantRepository.save(requester);
        }

        List<Long> participantMemberIds = partyParticipantRepository.findAllByPartyId(requester.getPartyId())
                .stream().map(PartyParticipant::getMemberId).toList();

        return new UnreadyDreamResult(participantMemberIds, requester.getMemberId(), false);
    }

    public LeavePartyResult leaveParty(LeavePartyCommand command) {
        PartyParticipant requester = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        String partyId = requester.getPartyId();

        // 드림 매칭 대기열에 있으면 나가기 불가
        Set<WaitingParty> waiting = waitingPartyRedisTemplate.opsForZSet()
                .range(WAITING_PARTY_KEY,0,-1);

        boolean inQueue = waiting != null && waiting.stream()
                .anyMatch(waitingParty -> Objects.equals(waitingParty.getPartyId(), partyId));
        if (inQueue){
            throw new GgumtleException(DreamErrorCode.CANNOT_LEAVE_WHILE_MATCHING);
        }

        List<PartyParticipant> members = partyParticipantRepository.findAllByPartyId(partyId);
        List<Long> memberIds = members.stream().map(PartyParticipant::getMemberId).toList();

        boolean wasLeader = requester.isLeader();
        Long leftMemberId = requester.getMemberId();

        partyParticipantRepository.delete(requester);
        Long newLeaderId = null;

        // 나가는 참가자가 리더라면 다른사람에게 리더 양도
        if (wasLeader){
            List<PartyParticipant> others = members.stream()
                    .filter(participant -> !participant.getMemberId().equals(leftMemberId))
                    .toList();
            if (!others.isEmpty()){
                PartyParticipant newLeader = others.stream()
                        .min(Comparator.comparing(PartyParticipant::getMemberId))
                        .get();
                newLeader.setAsLeader();
                partyParticipantRepository.save(newLeader);
                newLeaderId = newLeader.getMemberId();
            }
        }

        return new LeavePartyResult(memberIds, leftMemberId, newLeaderId);
    }
}
