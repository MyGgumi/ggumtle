package com.ggumtle.ggumtle.auth.application.result;

public record SignUpResult(
        Long memberId,
        String accessToken
) {
}
