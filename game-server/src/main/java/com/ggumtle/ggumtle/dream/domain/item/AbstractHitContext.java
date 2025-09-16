package com.ggumtle.ggumtle.dream.domain.item;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

@AllArgsConstructor(access = AccessLevel.PROTECTED)
public abstract class AbstractHitContext {
    protected Position source;

    protected Position target;
}
