package com.ggumtle.ggumtle.mission.presentation;

import com.ggumtle.ggumtle.common.LoginUser;
import com.ggumtle.ggumtle.mission.application.MissionService;
import com.ggumtle.ggumtle.mission.application.command.GetMissionsCommand;
import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;
import com.ggumtle.ggumtle.mission.presentation.response.GetMissionsResponse;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
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
}
