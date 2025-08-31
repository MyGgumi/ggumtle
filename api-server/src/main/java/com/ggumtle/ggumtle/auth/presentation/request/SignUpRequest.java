package com.ggumtle.ggumtle.auth.presentation.request;

import com.ggumtle.ggumtle.auth.application.command.SignUpCommand;
import jakarta.validation.constraints.NotBlank;

import java.time.LocalDate;

public record SignUpRequest(
        @NotBlank(message = "idToken은 필수 입력값입니다.")
        String idToken,
        @NotBlank(message = "이름은 필수 입력값입니다.")
        String name,
        @NotBlank(message = "닉네임은 필수 입력값입니다.")
        String nickname,
        LocalDate birthDate
) {
    public SignUpCommand toCommand(){
        return new SignUpCommand(idToken, name, nickname, birthDate);
    }
}
