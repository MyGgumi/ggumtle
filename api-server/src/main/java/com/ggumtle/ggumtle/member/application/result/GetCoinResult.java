package com.ggumtle.ggumtle.member.application.result;

import com.ggumtle.ggumtle.member.domain.Member;

public record GetCoinResult(
        Long memberId,
        int coin
) {

}
