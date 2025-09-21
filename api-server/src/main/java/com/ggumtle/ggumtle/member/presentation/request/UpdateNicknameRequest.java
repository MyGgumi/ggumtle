package com.ggumtle.ggumtle.member.presentation.request;

import com.ggumtle.ggumtle.member.application.command.UpdateNicknameCommand;
import jakarta.validation.constraints.NotBlank;

public record UpdateNicknameRequest(
        @NotBlank(message = "nickname은 필수 입력값입니다.")
        String nickname
) {
    public UpdateNicknameCommand toCommand(Long memberId){
        return  new UpdateNicknameCommand(memberId, nickname);
    }
}
