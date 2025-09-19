package com.ggumtle.ggumtle.dream.presentation.request;

import com.ggumtle.ggumtle.dream.application.command.ChangeMonggingCommand;

public record ChangeMonggingClassRequest(
        Long monggingId
) {
    public ChangeMonggingCommand toCommand(Long memberId) {
        return new ChangeMonggingCommand(memberId, monggingId);
    }
}
