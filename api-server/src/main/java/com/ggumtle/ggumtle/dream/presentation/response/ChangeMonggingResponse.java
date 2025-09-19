package com.ggumtle.ggumtle.dream.presentation.response;

import com.ggumtle.ggumtle.dream.application.result.ChangeMonggingResult;

public record ChangeMonggingResponse(
        Long memberId,
        Long classId,
        Integer level
) {
    public static ChangeMonggingResponse from(ChangeMonggingResult result) {
        return new ChangeMonggingResponse(
                result.memberId(),
                result.monggingClassId(),
                result.level()
        );
    }
}
