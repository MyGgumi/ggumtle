package com.ggumtle.ggumtle.mission.presentation.response;

import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;

import java.util.List;

public record GetMissionsResponse(
        List<Mission> missions
) {
    public static GetMissionsResponse from(GetMissionsResult result) {
        return new GetMissionsResponse(
                result.missions().stream()
                        .map(Mission::from)
                        .toList()
        );
    }

    public record Mission(
            Long memberMissionId,
            String name,
            String description,
            Integer requiredCount,
            Integer doneCount,
            String state
    ) {
        public static Mission from(GetMissionsResult.Mission result) {
            return new Mission(
                    result.memberMissionId(),
                    result.name(),
                    result.description(),
                    result.requiredCount(),
                    result.doneCount(),
                    result.state().name()
            );
        }
    }
}
