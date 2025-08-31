package com.ggumtle.ggumtle.auth.presentation.response;

import com.ggumtle.ggumtle.auth.application.result.SignUpResult;

public record SignUpResponse(
        Long memberId,
        String accessToken
) {
    public static SignUpResponse from(SignUpResult result){
        return new SignUpResponse(result.memberId(), result.accessToken());
    }
}
