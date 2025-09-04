package com.ggumtle.ggumtle.dream.vo;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.ToString;

@AllArgsConstructor
@Getter
@ToString
public final class Position {
    public final int x;

    public final int y;

    public final int z;

    public static Position from(Spawn spawn) {
        return new Position(spawn.getX(), spawn.getY(), spawn.getZ());
    }
}
