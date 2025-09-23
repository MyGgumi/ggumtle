package com.ggumtle.ggumtle.mongging.presentation.response;

import com.ggumtle.ggumtle.mongging.application.result.EnhanceMonggingResult;
import lombok.AccessLevel;
import lombok.Builder;

@Builder(access = AccessLevel.PRIVATE)
public record EnhanceMonggingResponse(
    boolean isSuccess,
    Experience experience
) {
    public static EnhanceMonggingResponse from(EnhanceMonggingResult result) {
        Experience experience = Experience.builder()
            .monggingId(result.monggingId())
            .statisticName(result.statisticName())
            .beforePercentage(result.beforePercentage())
            .afterPercentage(result.afterPercentage())
            .beforeLevel(result.beforeLevel())
            .afterLevel(result.afterLevel())
            .nextSuccessRate(result.nextSuccessRate())
            .build();

        return EnhanceMonggingResponse.builder()
            .isSuccess(result.isSuccess())
            .experience(experience)
            .build();
    }

    @Builder(access = AccessLevel.PRIVATE)
    record Experience(
        long monggingId,
        String statisticName,
        double beforePercentage,
        double afterPercentage,
        int beforeLevel,
        int afterLevel,
        int nextSuccessRate
    ) {
    }
}
