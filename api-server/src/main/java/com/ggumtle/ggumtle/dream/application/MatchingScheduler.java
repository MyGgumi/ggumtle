package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.dream.application.event.MatchingCancelledEvent;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.domain.WaitingParty;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import com.ggumtle.ggumtle.dream.presentation.response.MatchingCancelledResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Set;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MatchingScheduler {
    private static final String WAITTING_PARTY_KEY = "waiting_party";
    private final RedisTemplate<String, WaitingParty> waitingPartyRedisTemplate;
    private final ApplicationEventPublisher applicationEventPublisher;
    private final PartyParticipantRepository partyParticipantRepository;

    @Scheduled(fixedDelay = 5000)
    public void cancelExpiredMatching(){
        // 현재 시간 기준으로 만료된 파티가 있는지 확인
        long now = System.currentTimeMillis();

        // score가 '현재 시간'보다 작은 파티를 모두 조회
        Set<WaitingParty> expiredParties = waitingPartyRedisTemplate.opsForZSet()
                .rangeByScore(WAITTING_PARTY_KEY, 0, now);

        if(expiredParties == null || expiredParties.isEmpty()){
            return;
        }

        for (WaitingParty expiredParty : expiredParties) {
            waitingPartyRedisTemplate.opsForZSet().remove(WAITTING_PARTY_KEY, expiredParty);
            List<PartyParticipant> participants = partyParticipantRepository.findAllByPartyId(expiredParty.getPartyId());

            if(!participants.isEmpty()){
                List<Long> memberIds = participants.stream().map(PartyParticipant::getMemberId).toList();
                String message = "대기 시간이 초과되어 매칭이 취소되었습니다";

                MatchingCancelledResponse response = new MatchingCancelledResponse(message);

                SendSocketEvent event = new SendSocketEvent(SocketType.MATCHING_CANCELLED, memberIds, response);
                applicationEventPublisher.publishEvent(event);
            }
        }
    }
}
