package com.ggumtle.ggumtle.mongging.application.result;

import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import lombok.AccessLevel;
import lombok.Builder;

import java.util.List;

@Builder(access = AccessLevel.PRIVATE)
public record EnhanceMonggingResult(
    boolean isSuccess,
    long monggingId,
    String statisticName,
    double beforePercentage,
    double afterPercentage,
    int beforeLevel,
    int afterLevel
) {

    public static EnhanceMonggingResult ofSuccess(Mongging mongging, List<EnhanceStat> enhanceStats) {
        var beforeStat = enhanceStats.get(0);
        var afterStat = enhanceStats.get(1);

        return EnhanceMonggingResult.of(
                true,
                mongging.getId(),
                mongging.getMonggingClass().getName(),
                // 강화에 성공하면, 이전 수치에서 현재 수치가 됨
                beforeStat.getEnhancePercentage(),
                afterStat.getEnhancePercentage(),
                // 강화에 성공하면, 강화 전 level에서 현재 몽깅이 level이 됨
                beforeStat.getLevel(),
                mongging.getLevel());
    }

    public static EnhanceMonggingResult ofFail(Mongging mongging, List<EnhanceStat> enhanceStats) {
        var beforeStat = enhanceStats.get(0);

        return EnhanceMonggingResult.of(
                false,
                mongging.getId(),
                mongging.getMonggingClass().getName(),
                beforeStat.getEnhancePercentage(),
                beforeStat.getEnhancePercentage(),
                beforeStat.getLevel(),
                beforeStat.getLevel());
    }

    public static EnhanceMonggingResult of(
            boolean isSuccess,
            long monggingId,
            String statisticName,
            double beforePercentage,
            double afterPercentage,
            int beforeLevel,
            int afterLevel) {
        return EnhanceMonggingResult.builder()
            .isSuccess(isSuccess)
            .monggingId(monggingId)
            .statisticName(statisticName)
            .beforePercentage(beforePercentage)
            .afterPercentage(afterPercentage)
            .beforeLevel(beforeLevel)
            .afterLevel(afterLevel)
            .build();
    }

    @Builder(access = AccessLevel.PRIVATE)
    record Experience(
        long monggingId,
        String statisticName,
        double beforePercentage,
        double afterPercentage,
        int beforeLevel,
        int afterLevel
    ) {
    }
}
