package com.ggumtle.ggumtle.messaging.message;

import com.ggumtle.ggumtle.common.event.DreamEndEvent;

import java.util.List;

public record EndDreamMessage(
        long roomId,
        List<PlayerState> playerStates
) {
    public static EndDreamMessage from(DreamEndEvent event) {
        return new EndDreamMessage(
                event.roomId(),
                event.playerStates().stream().map(PlayerState::from).toList()
        );
    }

    public record PlayerState(
            long id,
            boolean isWinning
    ) {
        public static PlayerState from(DreamEndEvent.PlayerState event) {
            return new PlayerState(
                    event.id(),
                    event.isWinning()
            );
        }
    }
}
