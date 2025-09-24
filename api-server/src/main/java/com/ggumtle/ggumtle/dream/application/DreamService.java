package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.common.event.MemberDreamOutEvent;
import com.ggumtle.ggumtle.dream.application.command.CancelMatchingCommand;
import com.ggumtle.ggumtle.dream.application.command.StartDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.CancelMatchingResult;
import com.ggumtle.ggumtle.dream.application.result.StartDreamResult;
import com.ggumtle.ggumtle.dream.domain.Dream;
import com.ggumtle.ggumtle.dream.domain.OptimalServer;
import com.ggumtle.ggumtle.dream.domain.WaitingParty;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.persistence.DreamRepository;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.DreamErrorCode;
import com.ggumtle.ggumtle.messaging.RoomMessageManager;
import com.ggumtle.ggumtle.messaging.event.CreatedRoomEvent;
import com.ggumtle.ggumtle.messaging.event.EndDreamEvent;
import com.ggumtle.ggumtle.messaging.payload.RequestRoomPayload;
import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;
import com.ggumtle.ggumtle.mongging.persistence.EnhanceStatRepository;
import com.ggumtle.ggumtle.mongging.persistence.po.MonggingStatPo;
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
    private final EnhanceStatRepository enhanceStatRepository;
    private final RedisTemplate<String, OptimalServer> optimalServerRedisTemplate;

    /**
     * 드림을 시작한다
     * 요청한 파티가 드림 플레이어 인원 수를 충족하지 못할 경우 다른 파티들과 매칭한다
     * 매칭에 실패한 경우 매칭을 기다린다
     * 요청 수신과 매칭 기다리기, 매칭 성공, 드림 시작과 같은 전 과정에 대한 정보를 이벤트로 발행한다
     */
    public void startDream(StartDreamCommand command) {
        publishEvent(SocketType.START_DREAM, List.of(command.requesterId()), StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.RECEIVED));

        // 리더의 요청인지 확인
        PartyParticipant requester = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));
        if (!requester.isLeader()) {
            throw new GgumtleException(DreamErrorCode.NOT_LEADER, "파티의 리더만 드림을 시작할 수 있습니다");
        }

        // 모든 파티원이 준비 중인지 확인
        String partyId = requester.getPartyId();
        List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(partyId);
//        boolean allReady = participants.stream().allMatch(PartyParticipant::isReady);
//        if (!allReady) {
//            throw new GgumtleException(DreamErrorCode.NOT_ALL_READY);
//        }

        // 중복 시작 방지 - 이미 매칭 중인 파티인지 확인
        Set<WaitingParty> waitingParties = waitingPartyRedisTemplate.opsForZSet().range(WAITING_PARTY_KEY, 0, -1);
        if (waitingParties != null) {
            boolean alreadyInMatch = waitingParties.stream().anyMatch(waitingParty -> waitingParty.getPartyId().equals(partyId));
            if (alreadyInMatch) {
                throw new GgumtleException(DreamErrorCode.ALREADY_MATCHING);
            }
        }

        // 드림 시작 요청을 수락해 매칭 시작
        List<Long> requesterPartyParticipantIds = convertToId(participants);
        publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.START_MATCH));

        // 요청한 파티가 드림 플레이어 인원 수와 일치하면 바로 시작
        if (participants.size() == DREAM_PLAYER_SIZE) {
            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.MATCHED));
            log.info("요청한 파티가 인원 수와 일치해 바로 시작함");

            requestRoom(participants);
            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.CREATE_ROOM));
            return;
        }

        // 인원이 부족하면 다른 파티와 매칭 시도
        List<WaitingParty> matchedParties = matchWithWaitingParties(participants.size());
        log.info("요청한 파티의 매칭 결과: {}", matchedParties);

        // 매칭 실패 시 대기열에 추가해 매칭 기다리기
        if (matchedParties.isEmpty()) {
            WaitingParty waitingParty = new WaitingParty(participants.getFirst().getPartyId(), participants.size());
            waitingPartyRedisTemplate.opsForZSet().add(WAITING_PARTY_KEY, waitingParty, System.currentTimeMillis());
            log.info("매칭 실패 - 대기열에서 대기");

            publishEvent(SocketType.START_DREAM, requesterPartyParticipantIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.WAITING));
            return;
        }

        // 매칭 성공 시 매칭된 파티를 대기열에서 삭제하고 드림 시작
        List<PartyParticipant> players = extendAndDeleteMatchedParty(participants, matchedParties);
        List<Long> playerIds = convertToId(players);
        publishEvent(SocketType.START_DREAM, playerIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.MATCHED));
        log.info("매칭 성공 - 드림 시작");

        requestRoom(players);
        publishEvent(SocketType.START_DREAM, playerIds, StartDreamResult.beforeRoomCreation(StartDreamResult.START_DREAM_STATUS.CREATE_ROOM));
    }

    /**
     * 매칭 취소 요청 처리
     */
    public CancelMatchingResult cancelMatching(CancelMatchingCommand command) {
        PartyParticipant requester = partyParticipantRepository.findById(command.requesterId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_PARTY));

        List<PartyParticipant> allParticipants = partyParticipantRepository.findAllByPartyId(requester.getPartyId());
        WaitingParty waitingParty = new WaitingParty(requester.getPartyId(), allParticipants.size());

        Long removedCount = waitingPartyRedisTemplate.opsForZSet().remove(WAITING_PARTY_KEY, waitingParty);
        if (removedCount == null || removedCount == 0) {
            throw new GgumtleException(DreamErrorCode.NOT_FOUND_MATCHING);
        }

        List<Long> participantIds = allParticipants.stream().
                map(PartyParticipant::getMemberId).
                toList();

        return new CancelMatchingResult(participantIds, "매칭이 취소되었습니다");
    }

    /**
     * 드림을 플레이할 방이 생겼을 때 핸들링
     *
     * @param event 방 생성 이벤트
     */
    @EventListener
    public void handleCreatedRoom(CreatedRoomEvent event) {
        Optional<Dream> optionalDream = dreamRepository.findByRoomRequestId(event.requestId());
        if (optionalDream.isEmpty()) {
            log.error("방 생성 응답에 대한 요청 없음");
            return;
        }

        Dream dream = optionalDream.get();
        dream.setId(event.roomId());
        dreamRepository.save(dream);

        StartDreamResult result = StartDreamResult.afterRoomCreation(StartDreamResult.START_DREAM_STATUS.START_DREAM, dream);
        publishEvent(SocketType.START_DREAM, dream.getPlayerIds(), result);
    }

    /**
     * 종료된 드림 핸들링
     * @param event 드림 종료 이벤트
     */
    @EventListener
    public void handleEndDream(EndDreamEvent event) {
        Dream dream = dreamRepository.findById(event.roomId())
                .orElseThrow(() -> new GgumtleException(DreamErrorCode.NOT_FOUND_DREAM));

        applicationEventPublisher.publishEvent(new MemberDreamOutEvent(dream.getPlayerIds()));

        // TODO: 종료된 드림에 대한 처리


        dreamRepository.delete(dream);
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

    private List<PartyParticipant> extendAndDeleteMatchedParty(List<PartyParticipant> requesterParticipants, List<WaitingParty> matchedParties) {
        List<PartyParticipant> result = new ArrayList<>(requesterParticipants);

        for (WaitingParty matchedParty : matchedParties) {
            result.addAll(partyParticipantRepository.findAllByPartyId(matchedParty.getPartyId()));

            waitingPartyRedisTemplate.opsForZSet().remove(WAITING_PARTY_KEY, matchedParty, System.currentTimeMillis());
        }

        return result;
    }

    private void requestRoom(List<PartyParticipant> participants) {
        OptimalServer dreamServer = optimalServerRedisTemplate.opsForValue().get(OptimalServer.KEY);
        if (dreamServer == null) {
            log.error("레디스에 서버가 없음");
            return;
        }

        String roomRequestId = UUID.randomUUID().toString();
        log.info("드림 서버 선정: 서버={}, 요청 ID={}", dreamServer, roomRequestId);

        Dream dream = new Dream(roomRequestId, participants.stream().map(PartyParticipant::getMemberId).toList(), dreamServer);
        dreamRepository.save(dream);

        List<MonggingStatPo> pos = enhanceStatRepository.findAllByMonggingIds(participants.stream().map(PartyParticipant::getMonggingId).toList());
        RequestRoomPayload payload = RequestRoomPayload.of(roomRequestId, pos);
        roomMessageManager.sendMessage(dreamServer, payload);

        log.info("{}번 드림 서버에 방 생성 요청 전송", dreamServer.getId());
    }

    private void publishEvent(SocketType socketType, List<Long> memberIds, Object data) {
        SendSocketEvent event = new SendSocketEvent(socketType, memberIds, data);
        applicationEventPublisher.publishEvent(event);
    }
}
