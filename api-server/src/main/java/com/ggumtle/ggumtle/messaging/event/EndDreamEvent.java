package com.ggumtle.ggumtle.messaging.event;

import com.ggumtle.ggumtle.messaging.message.EndDreamMessage;

import java.util.List;

public record EndDreamEvent(
        long roomId,
        List<PlayerState> playerStates
) {
    public static EndDreamEvent from(EndDreamMessage message) {
        return new EndDreamEvent(
                message.roomId(),
                message.playerStates().stream().map(EndDreamEvent.PlayerState::from).toList()
        );
    }

    public record PlayerState(
            long id,
            boolean isWinning
    ) {
        public static PlayerState from(EndDreamMessage.PlayerState message) {
            return new PlayerState(
                    message.id(),
                    message.isWinning()
            );
        }
    }
}
