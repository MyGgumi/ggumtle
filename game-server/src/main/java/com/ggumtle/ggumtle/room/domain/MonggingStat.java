package com.ggumtle.ggumtle.room.domain;

import lombok.Builder;

@Builder
public final class MonggingStat {
    public final long playerId;

    public final long monggingClassId;

    public final int additionalHp;

    public final int additionalHealSpeed;

    public final int additionalTaskSpeed;
}
