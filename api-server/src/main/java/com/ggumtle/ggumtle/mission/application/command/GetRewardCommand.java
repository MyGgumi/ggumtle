package com.ggumtle.ggumtle.mission.application.command;

public record GetRewardCommand(
        Long memberId,
        Long memberMissionId
) {
}
