package com.ggumtle.ggumtle.mission.application.result;

import com.ggumtle.ggumtle.mission.domain.MemberMission;

public record GetRewardResult(
    Integer reward,
    Integer currentCoin
) {
    public static GetRewardResult from(MemberMission memberMission) {
        return new GetRewardResult(
                memberMission.getMission().getRewardCoinAmount(),
                memberMission.getMember().getCoin()
        );
    }
}
