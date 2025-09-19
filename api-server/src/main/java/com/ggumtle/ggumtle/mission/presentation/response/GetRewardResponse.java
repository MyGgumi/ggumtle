package com.ggumtle.ggumtle.mission.presentation.response;

import com.ggumtle.ggumtle.mission.application.result.GetRewardResult;

public record GetRewardResponse(
        Integer reward,
        Integer currentCoin
) {
    public static GetRewardResponse from(GetRewardResult result) {
        return new GetRewardResponse(
                result.reward(),
                result.currentCoin()
        );
    }
}
