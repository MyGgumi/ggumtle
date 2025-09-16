package com.ggumtle.ggumtle.dream.domain.item;

import com.ggumtle.ggumtle.dream.util.VectorCalculator;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.dream.vo.Vector;

public final class Taser extends Boxable implements Attackable<Taser.HitContext> {
    public static final int range = 1000;

    public Taser(int id, int initialCount, int maxCapacityForBox, int maxCapacityForMongging) {
        super(id, initialCount, maxCapacityForBox, maxCapacityForMongging);
    }

    @Override
    public boolean detectHit(HitContext context) {
        return VectorCalculator.calculateHit(
                context.source,
                context.target,
                context.vector,
                range,
                context.targetSize
        );
    }

    public static final class HitContext extends AbstractHitContext {
        public final int targetSize;
        private final Vector vector;

        public HitContext(Position source, Position target, int targetSize, int x, int y, int z) {
            super(source, target);
            this.targetSize = targetSize;
            this.vector = new Vector(x, y, z);
        }
    }
}
