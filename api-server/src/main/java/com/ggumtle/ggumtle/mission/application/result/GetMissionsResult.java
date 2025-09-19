package com.ggumtle.ggumtle.mission.application.result;

import java.util.List;

public record GetMissionsResult(
    List<Mission> missions
) {
    public static GetMissionsResult of(List<Mission> missions) {
        return new GetMissionsResult(
                missions == null ? List.of() :
                        missions.stream()
                        .map(mission -> new Mission(
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
