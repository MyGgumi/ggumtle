package com.ggumtle.ggumtle.auth.presentation.request;

import com.ggumtle.ggumtle.auth.application.command.LoginCommand;
import jakarta.validation.constraints.NotBlank;

public record LoginRequest(
        @NotBlank(message = "idToken은 필수 입력값입니다.")
        String idToken
) {
    public LoginCommand toCommand(){
        return new LoginCommand(idToken);
    }
}
