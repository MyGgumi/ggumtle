package com.ggumtle.ggumtle.mongging.application.result;

import com.ggumtle.ggumtle.mongging.domain.EnhancePercentage;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import lombok.AccessLevel;
import lombok.Builder;

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

    public static EnhanceMonggingResult ofSuccess(Mongging mongging, EnhancePercentage enhancePercentage) {
        return EnhanceMonggingResult.of(
                true,
                mongging.getId(),
                mongging.getMonggingClass().getName(),
                // 강화에 성공하면, 현재 수치에서 다음 수치가 됨
                enhancePercentage.getCurrentPercentage(),
                enhancePercentage.getNextPercentage(),
                
                // 강화에 성공하면, 강화 확률의 level에서 몽깅이 level이 됨
                enhancePercentage.getLevel(),
                mongging.getLevel());
    }

    public static EnhanceMonggingResult ofFail(Mongging mongging, EnhancePercentage enhancePercentage) {
        return EnhanceMonggingResult.of(
                false,
                mongging.getId(),
                mongging.getMonggingClass().getName(),
                enhancePercentage.getCurrentPercentage(),
                enhancePercentage.getCurrentPercentage(),
                mongging.getLevel(),
                mongging.getLevel());
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
