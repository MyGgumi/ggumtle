package com.ggumtle.ggumtle.dream.domain.item;

import lombok.AccessLevel;
import lombok.AllArgsConstructor;

@AllArgsConstructor(access = AccessLevel.PROTECTED)
public abstract class Boxable {
    public final int id;

    public final int initialCount;

    public final int maxCapacityForBox;

    public final int maxCapacityForMongging;
}
