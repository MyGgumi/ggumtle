package com.ggumtle.ggumtle.mission.presentation;

import com.ggumtle.ggumtle.common.LoginUser;
import com.ggumtle.ggumtle.mission.application.MissionService;
import com.ggumtle.ggumtle.mission.application.command.DoMissionCommand;
import com.ggumtle.ggumtle.mission.application.command.GetMissionsCommand;
import com.ggumtle.ggumtle.mission.application.command.GetRewardCommand;
import com.ggumtle.ggumtle.mission.application.result.DoMissionResult;
import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;
import com.ggumtle.ggumtle.mission.application.result.GetRewardResult;
import com.ggumtle.ggumtle.mission.presentation.response.DoMissionResponse;
import com.ggumtle.ggumtle.mission.presentation.response.GetMissionsResponse;
import com.ggumtle.ggumtle.mission.presentation.response.GetRewardResponse;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;

@Controller
@RequestMapping("/mission")
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MissionController {
    private final MissionService missionService;

    @GetMapping
    public ResponseEntity<GetMissionsResponse> getMissions(@LoginUser Long memberId) {
        GetMissionsCommand command = new GetMissionsCommand(memberId);

        GetMissionsResult result = missionService.getMissions(command);

        GetMissionsResponse response = GetMissionsResponse.from(result);
        return ResponseEntity.ok(response);
    }

    @PatchMapping("/{member-mission-id}")
    public ResponseEntity<DoMissionResponse> doMission(@LoginUser Long memberId, @PathVariable("member-mission-id") Long memberMissionId) {
        DoMissionCommand command = new DoMissionCommand(memberId, memberMissionId);

        DoMissionResult result = missionService.doMission(command);

        DoMissionResponse response = DoMissionResponse.from(result);
        return ResponseEntity.ok(response);
    }

    @PatchMapping("/{member-mission-id}/reward")
    public ResponseEntity<GetRewardResponse> getReward(@LoginUser Long memberId, @PathVariable("member-mission-id") Long memberMissionId) {
        GetRewardCommand command = new GetRewardCommand(memberId, memberMissionId);

        GetRewardResult result = missionService.getReward(command);

        GetRewardResponse response = GetRewardResponse.from(result);
        return ResponseEntity.ok(response);
    }
}
