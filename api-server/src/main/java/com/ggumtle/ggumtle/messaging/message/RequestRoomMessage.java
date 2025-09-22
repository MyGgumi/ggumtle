package com.ggumtle.ggumtle.messaging.message;

import com.ggumtle.ggumtle.messaging.payload.RequestRoomPayload;

import java.util.List;

public record RequestRoomMessage(
        String requestId,
        List<Player> players
) {
    public static RequestRoomMessage of(RequestRoomPayload payload) {
        return new RequestRoomMessage(
                payload.requestId(),
                payload.players().stream().map(Player::from).toList()
        );
    }

    public record Player(
            Long id,
            String nickname,
            Long monggingClassId,
            Integer additionalHp,
            Integer additionalTaskSpeed,
            Integer additionalHealSpeed
    ) {
        public static Player from(RequestRoomPayload.Player payload) {
            return new Player(
                    payload.id(),
                    payload.nickname(),
                    payload.monggingClassId(),
                    payload.additionalHp(),
                    payload.additionalTaskSpeed(),
                    payload.additionalHealSpeed()
            );
        }
    }
}
