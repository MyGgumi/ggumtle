package com.ggumtle.ggumtle.dream.domain.item;

import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.util.concurrent.atomic.AtomicBoolean;

public final class FieldItem {
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

    public boolean isUsed() {
        return isUsed.get();
    }

    public boolean use() {
        return this.isUsed.compareAndExchange(false, true);
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
