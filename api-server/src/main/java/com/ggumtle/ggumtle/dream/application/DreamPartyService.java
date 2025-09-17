package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.GetPartyParticipantsCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.GetInvitationsCommand;
import com.ggumtle.ggumtle.dream.application.command.ReadyDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.GetPartyParticipantsResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.application.result.GetInvitationsResult;
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
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.stream.Collectors;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamPartyService {
    private static final String WAITING_PARTY_KEY = "waiting_party";
    private static final String ACTIVE_PARTY_INDEX_KEY = "party_participant:idx";
    private static final String PARTY_PARTICIPANT_KEY_PREFIX = "party_participant:partyId:";


    private final PartyParticipantRepository partyParticipantRepository;
    private final PartyInvitationRepository partyInvitationRepository;
    private final MemberRepository memberRepository;
    private final RedisTemplate<String, WaitingParty> waitingPartyRedisTemplate;
    private final RedisTemplate<String, String> redisTemplate;

    public CreatePartyResult createParty(CreatePartyCommand command) {
        if (partyParticipantRepository.existsById(command.memberId())) {
            throw new GgumtleException(DreamErrorCode.ALREADY_IN_PARTY);
        }

        PartyParticipant partyParticipant = new PartyParticipant(command.memberId(), UUID.randomUUID().toString(), true);
        partyParticipantRepository.save(partyParticipant);
        redisTemplate.opsForSet().add(ACTIVE_PARTY_INDEX_KEY, partyParticipant.getPartyId());

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

        Long inviteeId = command.inviteeId();
        Optional<Member> invitee = memberRepository.findById(inviteeId);
        String inviteeNickname = invitee.get().getNickname();
        return new InvitePartyResult(inviteeId, partyInvitation.getId(), inviteeNickname);
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

        Set<WaitingParty> waiting = waitingPartyRedisTemplate.opsForZSet()
                .range(WAITING_PARTY_KEY, 0, -1);

        boolean inQueue = waiting != null && waiting.stream()
                .anyMatch(waitingParty -> Objects.equals(waitingParty.getPartyId(), partyId));
        if (inQueue) {
            throw new GgumtleException(DreamErrorCode.CANNOT_LEAVE_WHILE_MATCHING);
        }

        List<PartyParticipant> members = partyParticipantRepository.findAllByPartyId(partyId);
        List<Long> originalMemberIdsForEvent = members.stream().map(PartyParticipant::getMemberId).toList();

        partyParticipantRepository.delete(requester);
        Long newLeaderId = null;

        List<PartyParticipant> remainingMembers = members.stream()
                .filter(participant -> !participant.getMemberId().equals(command.requesterId()))
                .toList();

        if (remainingMembers.isEmpty()) {
            // 파티에 아무도 남지 않았다면,파티를 제거
            redisTemplate.opsForSet().remove(ACTIVE_PARTY_INDEX_KEY, partyId);
        } else if (requester.isLeader()) {
            // 나간 사람이 리더라면 새로운 리더를 위임
            PartyParticipant newLeader = remainingMembers.stream()
                    .min(Comparator.comparing(PartyParticipant::getMemberId))
                    .get();
            newLeader.setAsLeader();
            partyParticipantRepository.save(newLeader);
            newLeaderId = newLeader.getMemberId();
        }

        return new LeavePartyResult(originalMemberIdsForEvent, command.requesterId(), newLeaderId);
    }

    public GetInvitationsResult getInvitations(GetInvitationsCommand command) {
        Long requester = command.requesterId();

        List<PartyInvitation> invitations = partyInvitationRepository.findAllByInviteeId(requester);

        List<Long> inviterIds = invitations.stream()
                .map(PartyInvitation::getInviterId)
                .toList();

        Map<Long, String> inviterNicknameMap = memberRepository.findAllByIdIn(inviterIds).stream()
                .collect(Collectors.toMap(Member::getId, Member::getNickname));

        List<GetInvitationsResult.Invitation> invitationInfos = invitations.stream()
                .filter(invitation -> inviterNicknameMap.containsKey(invitation.getInviterId()))
                .map(invitation -> new GetInvitationsResult.Invitation(
                        invitation.getId(),
                        inviterNicknameMap.get(invitation.getInviterId())
                ))
                .toList();

        return new GetInvitationsResult(invitationInfos);
    }

    public GetPartyParticipantsResult getPartyParticipants(GetPartyParticipantsCommand command) {
        Long memberId = command.memberId();

        PartyParticipant requester = partyParticipantRepository.findById(memberId)
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));
        String partyId = requester.getPartyId();

        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(partyId);

        List<Long> memberIds = participants.stream()
                .map(PartyParticipant::getMemberId)
                .toList();

        Map<Long, Member> memberMap = memberRepository.findAllByIdIn(memberIds).stream()
                .collect(Collectors.toMap(Member::getId, m -> m));

        List<GetPartyParticipantsResult.Participant> participantDetails = participants.stream()
                .map(p -> {
                    Member memberInfo = memberMap.get(p.getMemberId());
                    String nickname = memberInfo.getNickname();

                    return new GetPartyParticipantsResult.Participant(
                            p.getMemberId(),
                            nickname,
                            p.isLeader(),
                            p.isReady()
                    );
                })
                .toList();

        return new GetPartyParticipantsResult(participantDetails);
    }


    public List<Long> getPartyMemberIds(Long memberId) {
        PartyParticipant participant = partyParticipantRepository.findById(memberId)
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        String partyId = participant.getPartyId();

        List<PartyParticipant> partyMembers = partyParticipantRepository.findAllByPartyId(partyId);

        return partyMembers.stream()
                .map(PartyParticipant::getMemberId)
                .toList();
    }
    public Set<Long> getAllActivePartyMemberIds() {
        Set<String> activePartyUuids = redisTemplate.opsForSet().members(ACTIVE_PARTY_INDEX_KEY);

        if (activePartyUuids == null || activePartyUuids.isEmpty()) {
            return Set.of();
        }

        List<String> partyParticipantKeys = activePartyUuids.stream()
                .map(uuid -> PARTY_PARTICIPANT_KEY_PREFIX + uuid)
                .toList();

        Set<String> memberIdsAsString = redisTemplate.opsForSet().union(partyParticipantKeys);

        if (memberIdsAsString == null || memberIdsAsString.isEmpty()) {
            return Set.of();
        }

        return memberIdsAsString.stream()
                .map(Long::parseLong)
                .collect(Collectors.toSet());
    }
}
