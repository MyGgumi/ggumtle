package com.ggumtle.ggumtle.mongging.presentation.response;

import com.ggumtle.ggumtle.mongging.application.result.MonggingDetailResult;
import lombok.AccessLevel;
import lombok.Builder;

@Builder(access = AccessLevel.PRIVATE)
public record MonggingDetailResponse(
    Long id,
    String monggingClass,
    Integer nowLevel,
    Double nowPercentage,
    boolean isMaxLevel,
    Integer afterLevel,
    Double afterPercentage,
    Integer needCoin,
    Integer successPercentage
) {
    public static MonggingDetailResponse from(MonggingDetailResult result) {
        return MonggingDetailResponse.builder()
            .id(result.monggingId())
            .monggingClass(result.monggingClassName())
            .nowLevel(result.nowLevel())
            .nowPercentage(result.nowPercentage())
            .isMaxLevel(result.isMaxLevel())
            .afterLevel(result.afterLevel())
            .afterPercentage(result.afterPercentage())
            .needCoin(result.needCoin())
            .successPercentage(result.successPercentage())
            .build();
    }
}
