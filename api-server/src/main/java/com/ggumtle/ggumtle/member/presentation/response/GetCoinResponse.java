package com.ggumtle.ggumtle.member.presentation.response;

public record GetCoinResponse(
        Long memberId,
        int coin
) {
}
