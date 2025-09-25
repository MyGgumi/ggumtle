package com.ggumtle.ggumtle.dream.domain.item;

import lombok.ToString;

@ToString
public final class Defibrillator extends Boxable {

    public Defibrillator(int id, int initialCount, int maxCapacityForBox, int maxCapacityForMongging) {
        super(id, initialCount, maxCapacityForBox, maxCapacityForMongging);
    }
}
