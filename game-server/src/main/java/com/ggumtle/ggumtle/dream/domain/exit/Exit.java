package com.ggumtle.ggumtle.dream.domain.exit;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.ToString;

@ToString
public class Exit {
    private static final int EXIT_SIZE = 100_000_000;

    public final int id;

    public final Position leftTop;
    public final Position rightBottom;

    public Exit(int id, Position middle) {
        this.id = id;
        int halfSize = EXIT_SIZE / 2;
        this.leftTop = new Position(middle.x - halfSize, middle.y - halfSize, middle.z - halfSize, middle.timestamp);
        this.rightBottom = new Position(middle.x + halfSize, middle.y + halfSize, middle.z + halfSize, middle.timestamp);
    }

    /**
     * 탈출구 범위 내에 있는지 확인
     * @param position 몽깅이 위치
     * @return 탈출구 범위 내에 있는지 여부
     */
    public boolean detectEscape(Position position) {
        return position.x >= leftTop.x && position.x <= rightBottom.x
                && position.z >= leftTop.z && position.z <= rightBottom.z;
    }
}
