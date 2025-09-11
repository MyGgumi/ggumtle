package com.ggumtle.ggumtle.dream.vo;

import lombok.AllArgsConstructor;
import lombok.Getter;

@AllArgsConstructor
@Getter
public enum Item {
    GGUMTLE_FEED(1, 60, 6, 99),
    FLASH(2, 3, 1, 3),
    TASER(3, 3, 1, 3),
    ;

    private final int id;
    private final int initialCount;
    private final int maxCapacityForBox;
    private final int maxCapacityForMongging;

    public static Item valueOf(int id) {
        for (Item item : values()) {
            if (item.id == id) {
                return item;
            }
        }
        return null;
    }

    public boolean isAttackItem() {
        if (id == 1) {
            return false;
        }

        return true;
    }
}
