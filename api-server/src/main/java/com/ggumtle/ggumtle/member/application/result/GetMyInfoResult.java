package com.ggumtle.ggumtle.member.application.result;

import com.ggumtle.ggumtle.member.domain.Member;

public record GetMyInfoResult(
        String nickname,
        int coin
) {
    public static GetMyInfoResult from(Member member) {
        return new GetMyInfoResult(member.getNickname(), member.getCoin());
    }
}
