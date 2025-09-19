package com.ggumtle.ggumtle.member.presentation.response;

import com.ggumtle.ggumtle.member.application.result.GetMyInfoResult;

public record GetMyInfoResponse(
        String nickname,
        int coin
) {
    public static GetMyInfoResponse from(GetMyInfoResult result) {
        return new GetMyInfoResponse(result.nickname(), result.coin());
    }
}
