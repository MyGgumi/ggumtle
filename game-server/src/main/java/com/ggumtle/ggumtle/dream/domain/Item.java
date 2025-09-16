package com.ggumtle.ggumtle.dream.domain;

import lombok.AllArgsConstructor;

@AllArgsConstructor
public enum Item {
    LIGHT_JELLY(1, 60, 6, 99),
    FLASH(2, 3, 1, 3),
    TASER(3, 3, 1, 3),
    ;

    public final int id;
    public final int initialCount;
    public final int maxCapacityForBox;
    public final int maxCapacityForMongging;

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
