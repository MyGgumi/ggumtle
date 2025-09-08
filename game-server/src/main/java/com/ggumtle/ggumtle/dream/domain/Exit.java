package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.ToString;

@ToString
public class Exit {
    private static final int EXIT_SIZE = 20;

    public final int id;

    public final Position leftTop;
    public final Position rightBottom;

    public Exit(int id, Position middle) {
        this.id = id;
        this.leftTop = new Position(middle.x, middle.y, middle.z, 0);
        this.rightBottom = new Position(middle.x, middle.y, middle.z, 0);
    }
}
