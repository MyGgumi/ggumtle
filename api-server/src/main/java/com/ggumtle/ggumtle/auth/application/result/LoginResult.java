package com.ggumtle.ggumtle.auth.application.result;

public record LoginResult(
        Long memberId,
        String accessToken
) {
}
