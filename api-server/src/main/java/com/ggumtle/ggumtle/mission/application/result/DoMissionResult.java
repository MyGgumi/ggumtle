package com.ggumtle.ggumtle.mission.application.result;

import com.ggumtle.ggumtle.mission.domain.MemberMission;
import com.ggumtle.ggumtle.mission.domain.MissionState;

public record DoMissionResult(
        Long memberMissionId,
        Integer requiredCount,
        Integer doneCount,
        MissionState state
) {
    public static DoMissionResult from(MemberMission memberMission) {
        return new DoMissionResult(
                memberMission.getId(),
                memberMission.getMission().getRequiredCount(),
                memberMission.getDoneCount(),
                memberMission.getState()
        );
    }
}
