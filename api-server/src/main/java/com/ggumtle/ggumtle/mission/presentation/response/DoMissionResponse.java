package com.ggumtle.ggumtle.mission.presentation.response;

import com.ggumtle.ggumtle.mission.application.result.DoMissionResult;

public record DoMissionResponse(
        Long memberMissionId,
        Integer requiredCount,
        Integer doneCount,
        String state
) {
    public static DoMissionResponse from(DoMissionResult result) {
        return new DoMissionResponse(
                result.memberMissionId(),
                result.requiredCount(),
                result.doneCount(),
                result.state().name()
        );
    }
}
