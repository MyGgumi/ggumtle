package com.ggumtle.ggumtle.dream.domain.item;

public interface Attackable<T extends AbstractHitContext> {
    boolean detectHit(T context);
}
