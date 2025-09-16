package com.ggumtle.ggumtle.dream.vo;

import lombok.AllArgsConstructor;
import lombok.ToString;

@AllArgsConstructor
@ToString
public final class Position {
    public final int x;

    public final int y;

    public final int z;

    public final long timestamp;

    public static Position from(Spawn spawn) {
        return new Position(spawn.getX(), spawn.getY(), spawn.getZ(), System.currentTimeMillis());
    }

    public int getDistanceSquareWith(Position other) {
        int dx = other.x - this.x;
        int dy = other.y - this.y;
        int dz = other.z - this.z;

        return dx * dx + dy * dy + dz * dz;
    }
}
