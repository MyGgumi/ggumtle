package com.ggumtle.ggumtle.messaging.message;

import com.ggumtle.ggumtle.dream.domain.PartyParticipant;

import java.util.List;

public record RequestRoomMessage(
        String requestId,
        List<Player> players
) {
    public static RequestRoomMessage of(String requestId, List<PartyParticipant> participants) {
        return new RequestRoomMessage(requestId, participants.stream().map(Player::from).toList());
    }

    public record Player(
            Long id,
            Long monggingClassId,
            Integer additionalHp,
            Integer additionalTaskSpeed,
            Integer additionalHealSpeed
    ) {
        // TODO: 몽깅이의 클래스와 레벨을 기반으로 추가 능력치 계산값을 반환
        public static Player from(PartyParticipant participant) {
            return new Player(
                    participant.getMemberId(),
                    participant.getMonggingId(),
                    0,
                    0,
                    0
            );
        }
    }
}
