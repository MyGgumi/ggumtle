package com.ggumtle.ggumtle.dream.domain.item;

import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.util.concurrent.atomic.AtomicBoolean;

public final class FieldItem {
    public static final int FIELD_ITEM_SIZE = 10;

    public final int id;

    public final Type type;

    public final Position position;

    private final AtomicBoolean isUsed;

    public FieldItem(FieldItemSpawn spawn) {
        this.id = spawn.getId();
        this.type = Type.valueOf(spawn.getTypeId());
        this.position = Position.from(spawn);
        this.isUsed = new AtomicBoolean(false);
    }

    /**
     * 필드 아이템이 이미 사용되었는지 확인
     * @return 이미 사용되었는지 여부
     */
    public boolean isUsed() {
        return isUsed.get();
    }

    /**
     * 필드 아이템 사용
     * @return 사용 성공 여부
     */
    public boolean use() {
        return this.isUsed.compareAndExchange(false, true);
    }

    /**
     * 필드 아이템 근처인지 확인
     * @param position
     * @return 근처 여부
     */
    public boolean detectPosition(Position position) {
        return this.position.getDistanceSquareWith(position) <= FIELD_ITEM_SIZE;
    }

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Type {
        HEAL(1), SPEED(2);

        public final int id;

        public static Type valueOf(int id) {
            for (Type type : Type.values()) {
                if (type.id == id) {
                    return type;
                }
            }

            return null;
        }
    }
}
