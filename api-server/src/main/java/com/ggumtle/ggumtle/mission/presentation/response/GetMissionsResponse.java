package com.ggumtle.ggumtle.mission.presentation.response;

import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;

import java.util.List;

public record GetMissionsResponse(
        List<Mission> missions
) {
    public static GetMissionsResponse from(GetMissionsResult result) {
        return new GetMissionsResponse(
                result.missions().stream()
                        .map(mission ->
                            new Mission(
                                mission.userMissionId(),
                                mission.missionName(),
                                mission.missionDescription(),
                                mission.requiredCount(),
                                mission.nowCount(),
                                mission.state()))
                        .toList()
        );
    }

    public record Mission(
            Long userMissionId,
            String missionName,
            String missionDescription,
            int requiredCount,
            int nowCount,
            String state
    ){}
}
