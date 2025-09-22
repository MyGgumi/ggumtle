package com.ggumtle.ggumtle.common.event;

import com.ggumtle.ggumtle.dream.domain.player.Mongging;
import com.ggumtle.ggumtle.dream.domain.player.Player;

import java.util.Collection;
import java.util.List;

public record DreamEndEvent(
        long roomId,
        List<PlayerState> playerStates
) {
    public static DreamEndEvent of(long roomId, Collection<Player> players, boolean isMonggingWin) {
        return new DreamEndEvent(
                roomId,
                players.stream().map(player -> PlayerState.of(player, isMonggingWin)).toList()
        );
    }

    public record PlayerState(
            long id,
            boolean isWinning
    ) {
        public static PlayerState of(Player player, boolean isMonggingWin) {
            return new PlayerState(
                    player.getId(),
                    isMonggingWin == (player instanceof Mongging));
        }
    }
}
