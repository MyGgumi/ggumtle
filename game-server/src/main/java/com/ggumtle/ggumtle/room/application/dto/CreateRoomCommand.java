package com.ggumtle.ggumtle.room.application.dto;

import com.ggumtle.ggumtle.room.domain.PlayerInfo;
import lombok.Builder;

import java.util.List;

public record CreateRoomCommand(
        Long roomId,
        List<Player> players
) {
    @Builder
    public record Player(
            long playerId,
            String nickname,
            long monggingClassId,
            int additionalHp,
            int additionalTaskSpeed,
            int additionalHealSpeed
    ) {
        public PlayerInfo toDomain() {
            return PlayerInfo.builder()
                    .playerId(this.playerId)
                    .nickname(this.nickname)
                    .monggingClassId(this.monggingClassId)
                    .additionalHp(this.additionalHp)
                    .additionalHealSpeed(this.additionalHealSpeed)
                    .additionalTaskSpeed(this.additionalTaskSpeed)
                    .build();
        }
    }
}
