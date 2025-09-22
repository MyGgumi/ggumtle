package com.ggumtle.ggumtle.mongging.application.dto;

import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;

import java.util.List;

public record EnhanceStatsDto(
    Integer level,
    Long monggingClassId,
    Double currentPercentage,
    Double nextPercentage
) {
    
    public static EnhanceStatsDto from(List<EnhanceStat> enhanceStats) {
        if (enhanceStats.isEmpty()) {
            throw new IllegalArgumentException("EnhanceStat list cannot be empty");
        }
        
        var currentStat = enhanceStats.get(0);
        Double nextPercentage = null;
        
        // 다음 레벨이 있으면 nextPercentage 설정
        if (enhanceStats.size() > 1) {
            nextPercentage = enhanceStats.get(1).getEnhancePercentage();
        }
        
        return new EnhanceStatsDto(
            currentStat.getLevel(),
            currentStat.getMonggingClass().getId(),
            currentStat.getEnhancePercentage(),
            nextPercentage
        );
    }
}
