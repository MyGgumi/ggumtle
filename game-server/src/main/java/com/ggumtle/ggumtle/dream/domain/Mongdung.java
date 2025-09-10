package com.ggumtle.ggumtle.dream.domain;

import com.ggumtle.ggumtle.dream.vo.Position;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;
import lombok.Getter;

public class Mongdung extends Player {
    private static final int BASE_MOVE_SPEED = 100;
    private static final int BASE_DAMAGE = 40;
    private static final int BASE_SCARE_COOL_TIME = 30 * 1000;
    private static final int MAX_BURY_COUNT = 3;
    private static final double HIT_SIZE_X = 500;
    private static final double HIT_SIZE_Y = 1000;
    private static final double HIT_SIZE_Z = 500;
    private static final double TARGET_SIZE_X = 1000;
    private static final double TARGET_SIZE_Y = 2000;
    private static final double TARGET_SIZE_Z = 1000;

    protected int moveSpeed;
    private long lastScareTime;
    private int buryFakeGgumtleCount;

    @Getter
    protected int damage;

    public Mongdung(long id, Position position) {
        super(id, position);

        this.moveSpeed = BASE_MOVE_SPEED;
        this.damage = BASE_DAMAGE;
        this.lastScareTime = 0;
        this.buryFakeGgumtleCount = 0;
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

    public synchronized boolean scare() {
        long now = System.currentTimeMillis();

        if (this.lastScareTime + BASE_SCARE_COOL_TIME > now) {
            return false;
        }

        this.lastScareTime = now;
        return true;
    }

    public synchronized boolean tryBuryFakeGgumtle() {
        if (this.buryFakeGgumtleCount < MAX_BURY_COUNT) {
            this.buryFakeGgumtleCount++;
            return true;
        }

        return false;
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
