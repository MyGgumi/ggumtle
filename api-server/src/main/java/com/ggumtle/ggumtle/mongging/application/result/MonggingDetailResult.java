package com.ggumtle.ggumtle.mongging.application.result;

import com.ggumtle.ggumtle.mongging.domain.EnhanceConfig;
import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import lombok.AccessLevel;
import lombok.Builder;

import java.util.List;

@Builder(access = AccessLevel.PRIVATE)
public record MonggingDetailResult(
    Long monggingId,
    String monggingClassName,
    Integer nowLevel,
    Double nowPercentage,
    boolean isMaxLevel,
    Integer afterLevel,
    Double afterPercentage,
    Integer needCoin,
    Integer successPercentage
) {

    public static MonggingDetailResult of(Mongging mongging, List<EnhanceStat> enhanceStats, EnhanceConfig nextConfig) {
        var monggingClass = mongging.getMonggingClass();
        
        // 0~1 범위의 성공 확률을 0~100 정수 퍼센트로 변환
        var successPercentageInt = (int) Math.round(nextConfig.getSuccessPercentage() * 100);

        var currentStat = enhanceStats.get(0);
        var nextStat = enhanceStats.get(1);

        return MonggingDetailResult.builder()
            .monggingId(mongging.getId())
            .monggingClassName(monggingClass.getName())
            .nowLevel(mongging.getLevel())
            .nowPercentage(currentStat.getEnhancePercentage())
            .isMaxLevel(false)
            .afterLevel(mongging.getLevel() + 1)
            .afterPercentage(nextStat.getEnhancePercentage())
            .needCoin(nextConfig.getRequiredCoin())
            .successPercentage(successPercentageInt)
            .build();
    }

    public static MonggingDetailResult ofMaxLevel(Mongging mongging, List<EnhanceStat> enhanceStats) {
        var monggingClass = mongging.getMonggingClass();

        var enhanceStat = enhanceStats.get(0);

        return MonggingDetailResult.builder()
            .monggingId(mongging.getId())
            .monggingClassName(monggingClass.getName())
            .nowLevel(mongging.getLevel())
            .nowPercentage(enhanceStat.getEnhancePercentage())
            .isMaxLevel(true)
            .afterLevel(null)
            .afterPercentage(null)
            .needCoin(null)
            .successPercentage(null)
            .build();
    }
}
