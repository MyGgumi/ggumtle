package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.dream.application.command.StartDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.StartDreamResult;
import com.ggumtle.ggumtle.dream.domain.Dream;
import com.ggumtle.ggumtle.dream.domain.DreamServer;
import com.ggumtle.ggumtle.dream.domain.WaitingParty;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.persistence.DreamRepository;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.DreamErrorCode;
import com.ggumtle.ggumtle.messaging.RoomMessageManager;
import com.ggumtle.ggumtle.messaging.event.CreatedRoomEvent;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class DreamService {

    private static final Integer DREAM_PLAYER_SIZE = 4;
    private static final String WAITING_PARTY_KEY = "waiting_party";

    private final ApplicationEventPublisher applicationEventPublisher;
    private final PartyParticipantRepository partyParticipantRepository;
    private final RedisTemplate<String, WaitingParty> waitingPartyRedisTemplate;
    private final DreamRepository dreamRepository;
    private final RoomMessageManager roomMessageManager;

    /**
     * 드림을 시작한다
     * 요청한 파티가 드림 플레이어 인원 수를 충족하지 못할 경우 다른 파티들과 매칭한다
     * 매칭에 실패한 경우 매칭을 기다린다
     * 요청 수신과 매칭 기다리기, 매칭 성공, 드림 시작과 같은 전 과정에 대한 정보를 이벤트로 발행한다
     */
    public void startDream(StartDreamCommand command) {
        publishEvent(SocketType.START_DREAM, List.of(command.requesterId()), new StartDreamResult(StartDreamResult.START_DREAM_STATUS.RECEIVED, null));

        PartyParticipant leader = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        if (!leader.isLeader()) {
            throw new GgumtleException(DreamErrorCode.NOT_LEADER, "파티의 리더만 드림을 시작할 수 있습니다");
        }

        String partyId = leader.getPartyId();
        List<PartyParticipant> participantsInParty = partyParticipantRepository.findAllByPartyId(partyId);
        boolean allReady = participantsInParty.stream().allMatch(PartyParticipant::isReady);
        if (!allReady) {
            throw new GgumtleException((DreamErrorCode.NOT_ALL_READY)
            );
        }

        // 중복 시작 방지
        Set<WaitingParty> waitingParties = waitingPartyRedisTemplate.opsForZSet().range(WAITING_PARTY_KEY, 0, -1);
        if (waitingParties != null) {
            boolean alreadyInMatch = waitingParties.stream()
                    .anyMatch(waitingParty -> waitingParty.getPartyId().equals(partyId));
            if (alreadyInMatch) {
                throw new GgumtleException(DreamErrorCode.ALREADY_MATCHING);
            }
        }

        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(leader.getPartyId());
        List<Long> requesterPartyParticipantIds = convertToId(participants);
        publishEvent(
                SocketType.START_DREAM,
                requesterPartyParticipantIds,
                new StartDreamResult(StartDreamResult.START_DREAM_STATUS.START_MATCH, null));

        log.info("드림 시작을 요청한 파티원: {}", participants);

        // 요청한 파티가 드림 플레이어 인원 수와 일치하면 바로 시작
        if (participants.size() == DREAM_PLAYER_SIZE) {
            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, new StartDreamResult(StartDreamResult.START_DREAM_STATUS.MATCHED, null));

            requestRoom(requesterPartyParticipantIds);
            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, new StartDreamResult(StartDreamResult.START_DREAM_STATUS.CREATE_ROOM, null));
            return;
        }

        // 다른 파티와 매칭 시도
        List<WaitingParty> matchedParties = matchWithWaitingParties(participants.size());
        log.info("매칭 결과: {}", matchedParties);

        // 매칭 실패 시 대기열에 추가해 매칭 기다리기
        if (matchedParties.isEmpty()) {
            WaitingParty waitingParty = new WaitingParty(participants.getFirst().getPartyId(), participants.size());
            waitingPartyRedisTemplate.opsForZSet().add(WAITING_PARTY_KEY, waitingParty, System.currentTimeMillis() + 60000);

            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, new StartDreamResult(StartDreamResult.START_DREAM_STATUS.WAITING, null));
            return;
        }

        // 매칭 성공 시 매칭된 파티를 대기열에서 삭제하고 드림 시작
        List<Long> matchedParticipantIds = convertToId(participants, matchedParties);
        publishEvent(SocketType.START_DREAM, matchedParticipantIds, new StartDreamResult(StartDreamResult.START_DREAM_STATUS.MATCHED, null));

        for (WaitingParty matchedParty : matchedParties) {
            waitingPartyRedisTemplate.opsForZSet().remove(WAITING_PARTY_KEY, matchedParty, System.currentTimeMillis());
        }

        requestRoom(matchedParticipantIds);
        publishEvent(SocketType.START_DREAM, matchedParticipantIds, new StartDreamResult(StartDreamResult.START_DREAM_STATUS.CREATE_ROOM, null));
    }

    /**
     * 드림을 플레이할 방이 생겼을 때 핸들링
     *
     * @param event 방 생성 이벤트
     */
    @EventListener
    public void handleCreatedRoom(CreatedRoomEvent event) {
        Optional<Dream> dream = dreamRepository.findByRoomRequestId(event.requestId());
        if (dream.isEmpty()) {
            log.error("방 생성 응답에 대한 요청 없음");
            return;
        }

        StartDreamResult result = new StartDreamResult(StartDreamResult.START_DREAM_STATUS.START_DREAM, event.roomId(), event.dreamServerId());
        publishEvent(SocketType.START_DREAM, dream.get().getPlayerIds(), result);
    }

    /**
     * 매칭을 기다리는 다른 파티들과 매칭을 시도한다
     * 파티들이 매칭을 기다리기 시작한 순서대로 추가가 가능한지 확인한다
     *
     * @param partySize 매칭을 요청한 파티의 구성원 수
     * @return 매칭에 성공할 경우 매칭된 파티들을, 실패할 경우 빈 배열을 반환한다
     */
    private List<WaitingParty> matchWithWaitingParties(int partySize) {
        int currentSize = partySize;
        List<WaitingParty> matchedParties = new ArrayList<>();

        Set<WaitingParty> waitingParties = waitingPartyRedisTemplate.opsForZSet().range(WAITING_PARTY_KEY, 0, -1);
        for (WaitingParty waitingParty : waitingParties) {
            // 드림 플레이어 인원을 초과하면 무시
            if (currentSize + waitingParty.getParticipantSize() > DREAM_PLAYER_SIZE) {
                continue;
            }

            currentSize += waitingParty.getParticipantSize();
            matchedParties.add(waitingParty);

            // 드림 플레이어 인원이 부족하면 다음 파티 확인
            if (currentSize < DREAM_PLAYER_SIZE) {
                continue;
            }

            // 드림 플레이어 인원이 충족되면 매칭 종료
            return matchedParties;
        }

        return new ArrayList<>();
    }

    private List<Long> convertToId(List<PartyParticipant> participants) {
        return participants.stream().map(PartyParticipant::getMemberId).toList();
    }

    private List<Long> convertToId(List<PartyParticipant> participants, List<WaitingParty> matchedParties) {
        List<Long> ids = new ArrayList<>();

        for (PartyParticipant participant : participants) {
            ids.add(participant.getMemberId());
        }

        for (WaitingParty matchedParty : matchedParties) {
            List<PartyParticipant> matchedParticipants = partyParticipantRepository.findAllByPartyId(matchedParty.getPartyId());
            for (PartyParticipant matchedParticipant : matchedParticipants) {
                ids.add(matchedParticipant.getMemberId());
            }
        }

        return ids;
    }

    private void requestRoom(List<Long> playerIds) {
        // TODO: 요청할 드림 서버 선정
        DreamServer dreamServer = new DreamServer("1");
        String roomRequestId = UUID.randomUUID().toString();

        Dream dream = new Dream(roomRequestId, playerIds);
        dreamRepository.save(dream);

        roomMessageManager.sendMessage(dreamServer, roomRequestId, playerIds);
    }

    private void publishEvent(SocketType socketType, List<Long> memberIds, Object data) {
        SendSocketEvent event = new SendSocketEvent(socketType, memberIds, data);
        applicationEventPublisher.publishEvent(event);
    }
}
