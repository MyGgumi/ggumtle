package com.ggumtle.ggumtle.dream.application.command;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;

public record MoveItemCommand(
    byte direction,
    int boxId,
    int index
) {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum DIRECTION {
        BOX_TO_INVENTORY((byte) 1),
        INVENTORY_TO_BOX((byte) 2),
        ;

        public final byte value;

        public static DIRECTION valueOf(final byte value) {
            for (DIRECTION direction : DIRECTION.values()) {
                if (direction.value == value) {
                    return direction;
                }
            }
            return null;
        }
    }
}
