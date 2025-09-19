package com.ggumtle.ggumtle.mongging.application.result;

import com.ggumtle.ggumtle.mongging.domain.EnhancePercentage;
import com.ggumtle.ggumtle.mongging.domain.Mongging;
import lombok.AccessLevel;
import lombok.Builder;

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

    public static MonggingDetailResult of(Mongging mongging, EnhancePercentage currentEnhance, EnhancePercentage nextEnhance) {
        var monggingClass = mongging.getMonggingClass();
        
        // 0~1 범위의 성공 확률을 0~100 정수 퍼센트로 변환
        var successPercentageInt = (int) Math.round(nextEnhance.getSuccessPercentage() * 100);

        return MonggingDetailResult.builder()
            .monggingId(mongging.getId())
            .monggingClassName(monggingClass.getName())
            .nowLevel(mongging.getLevel())
            .nowPercentage(currentEnhance.getCurrentPercentage())
            .isMaxLevel(false)
            .afterLevel(mongging.getLevel() + 1)
            .afterPercentage(nextEnhance.getCurrentPercentage())
            .needCoin(nextEnhance.getRequiredCoin())
            .successPercentage(successPercentageInt)
            .build();
    }

    public static MonggingDetailResult ofMaxLevel(Mongging mongging, EnhancePercentage currentEnhance) {
        var monggingClass = mongging.getMonggingClass();

        return MonggingDetailResult.builder()
            .monggingId(mongging.getId())
            .monggingClassName(monggingClass.getName())
            .nowLevel(mongging.getLevel())
            .nowPercentage(currentEnhance.getCurrentPercentage())
            .isMaxLevel(true)
            .afterLevel(null)
            .afterPercentage(null)
            .needCoin(null)
            .successPercentage(null)
            .build();
    }
}
