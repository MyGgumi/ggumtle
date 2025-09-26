package com.ggumtle.ggumtle.dream.domain.player;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

public class Mongdung extends Player {
    private static final int BASE_MOVE_SPEED = 1_000_000_000;
    private static final int BASE_DAMAGE = 40;
    private static final int BASE_SCARE_COOL_TIME = 1 * 1000;
    private static final int MAX_BURY_COUNT = 3;
    public static final int SIZE = 10000;
    private static final double HIT_SIZE_X = 500;
    private static final double HIT_SIZE_Y = 1000;
    private static final double HIT_SIZE_Z = 500;
    private static final double TARGET_SIZE_X = 1000;
    private static final double TARGET_SIZE_Y = 2000;
    private static final double TARGET_SIZE_Z = 1000;

    private final AtomicLong lastScareTime;
    private final AtomicInteger buryFakeGgumtleCount;

    public final int damage;

    public Mongdung(long id, Position position) {
        super(id, position, BASE_MOVE_SPEED);

        this.damage = BASE_DAMAGE;
        this.lastScareTime = new AtomicLong(System.currentTimeMillis());
        this.buryFakeGgumtleCount = new AtomicInteger(0);
    }

    public boolean detectHit(int vx, int vy, int vz, long timestamp, Player target) {
        Position mongdungPosition = super.getPositionAt(timestamp);
        Position targetPosition = target.getPositionAt(timestamp);

        // 몽둥이 공격의 중심 좌표
        double hitCenterX = mongdungPosition.x + vx;
        double hitCenterY = mongdungPosition.y + vy;
        double hitCenterZ = mongdungPosition.z + vz;

        // 몽둥이의 공격 범위
        double hitMinX = hitCenterX - HIT_SIZE_X;
        double hitMaxX = hitCenterX + HIT_SIZE_X;
        double hitMinY = hitCenterY - HIT_SIZE_Y;
        double hitMaxY = hitCenterY + HIT_SIZE_Y;
        double hitMinZ = hitCenterZ - HIT_SIZE_Z;
        double hitMaxZ = hitCenterZ + HIT_SIZE_Z;

        // 타겟의 범위
        double targetMinX = targetPosition.x - TARGET_SIZE_X;
        double targetMaxX = targetPosition.x + TARGET_SIZE_X;
        double targetMinY = targetPosition.y - TARGET_SIZE_Y;
        double targetMaxY = targetPosition.y + TARGET_SIZE_Y;
        double targetMinZ = targetPosition.z - TARGET_SIZE_Z;
        double targetMaxZ = targetPosition.z + TARGET_SIZE_Z;

        // AABB-AABB 충돌 확인
        return (hitMinX <= targetMaxX && hitMaxX >= targetMinX) &&
                (hitMinY <= targetMaxY && hitMaxY >= targetMinY) &&
                (hitMinZ <= targetMaxZ && hitMaxZ >= targetMinZ);
    }

    public boolean scare() {
        long now = System.currentTimeMillis();

        long current = this.lastScareTime.get();
        if (current + BASE_SCARE_COOL_TIME > now) {
            return false;
        }

        return this.lastScareTime.compareAndSet(current, now);
    }

    public boolean tryBuryFakeGgumtle() {
        int current = this.buryFakeGgumtleCount.get();

        if (current < MAX_BURY_COUNT) {
            return true;
        }

        return this.buryFakeGgumtleCount.compareAndSet(current, current + 1);
    }

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum SkillType {
        SCARE(1), FAKE_GGUMTLE(2);

        @Getter
        private final int id;

        public static SkillType valueById(int id) {
            for (SkillType skillType : SkillType.values()) {
                if (skillType.id == id) {
                    return skillType;
                }
            }

            return null;
        }
    }
}
