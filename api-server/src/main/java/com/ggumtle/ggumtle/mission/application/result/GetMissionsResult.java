package com.ggumtle.ggumtle.mission.application.result;

import com.ggumtle.ggumtle.mission.domain.MemberMission;
import com.ggumtle.ggumtle.mission.domain.MissionState;

import java.util.List;

public record GetMissionsResult(
    List<Mission> missions
) {
    public static GetMissionsResult of(List<MemberMission> missions) {
        return new GetMissionsResult(
                missions == null ? List.of() :
                        missions.stream()
                        .map(memberMission -> new Mission(
                                memberMission.getId(),
                                memberMission.getMission().getId(),
                                memberMission.getMission().getName(),
                                memberMission.getMission().getDescription(),
                                memberMission.getMission().getRequiredCount(),
                                memberMission.getDoneCount(),
                                memberMission.getMission().getRewardCoinAmount(),
                                memberMission.getState()))
                        .toList()
        );
    }

    public record Mission(
            Long memberMissionId,
            Integer missionId,
            String name,
            String description,
            Integer requiredCount,
            Integer doneCount,
            Integer reward,
            MissionState state
    ){}
}
