package com.ggumtle.ggumtle.messaging.payload;

import com.ggumtle.ggumtle.mongging.persistence.po.MonggingStatPo;

import java.util.ArrayList;
import java.util.List;

public record RequestRoomPayload(
        String requestId,
        List<Player> players
) {
    public static RequestRoomPayload of(String requestId, List<MonggingStatPo> pos) {
        List<Player> players = new ArrayList<>();
        for (MonggingStatPo po : pos) {
            Player player = new Player(
                    po.memberId(),
                    po.nickname(),
                    po.monggingClassId(),
                    (int) (po.monggingClassId() == 1 ? po.additionalStat() : 0),
                    (int) (po.monggingClassId() == 2 ? po.additionalStat() : 0),
                    (int) (po.monggingClassId() == 3 ? po.additionalStat() : 0)
            );

            players.add(player);
        }

        return new RequestRoomPayload(
                requestId,
                players
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
    }
}
