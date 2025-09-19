package com.ggumtle.ggumtle.mission.application.command;

public record DoMissionCommand(
        Long memberId,
        Long memberMissionId
) {
}
