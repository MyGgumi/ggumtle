package com.ggumtle.ggumtle.auth.presentation.response;

import com.ggumtle.ggumtle.auth.application.result.LoginResult;

public record LoginResponse(
        Long memberId,
        String accessToken
) {
    public static LoginResponse from(LoginResult result){
        return new LoginResponse(
                result.memberId(),
                result.accessToken()
        );
    }
}
