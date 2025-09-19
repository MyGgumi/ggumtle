package com.ggumtle.ggumtle.dream.application.result;

import java.util.List;

public record ChangeMonggingResult(
        List<Long> participantIds,
        Long memberId,
        Long monggingId,
        Long monggingClassId,
        Integer level
) {
}
