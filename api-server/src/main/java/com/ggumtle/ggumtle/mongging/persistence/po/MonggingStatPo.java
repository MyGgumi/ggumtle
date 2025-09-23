package com.ggumtle.ggumtle.mongging.persistence.po;

public record MonggingStatPo(
        Long memberId,
        String nickname,
        Long monggingId,
        Integer monggingLevel,
        Long monggingClassId,
        Double additionalStat
) {
}
