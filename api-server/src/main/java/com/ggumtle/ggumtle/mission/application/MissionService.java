package com.ggumtle.ggumtle.mission.application;

import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.MissionErrorCode;
import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.mission.application.command.DoMissionCommand;
import com.ggumtle.ggumtle.mission.application.command.GetMissionsCommand;
import com.ggumtle.ggumtle.mission.application.command.GetRewardCommand;
import com.ggumtle.ggumtle.mission.application.result.DoMissionResult;
import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;
import com.ggumtle.ggumtle.mission.application.result.GetRewardResult;
import com.ggumtle.ggumtle.mission.domain.Mission;
import com.ggumtle.ggumtle.mission.domain.MemberMission;
import com.ggumtle.ggumtle.mission.domain.MissionState;
import com.ggumtle.ggumtle.mission.persistence.MemberMissionRepository;
import com.ggumtle.ggumtle.mission.persistence.MissionRepository;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class MissionService {
    private final MissionRepository missionRepository;
    private final MemberMissionRepository memberMissionRepository;

    @Transactional
    public void createMemberMission(Member newMember) {
        List<Mission> allMissions = missionRepository.findAll();

        List<MemberMission> missions = new ArrayList<>();
        for (Mission oneMission : allMissions) {
            missions.add(new MemberMission(newMember, oneMission));
        }
        memberMissionRepository.saveAll(missions);
    }

    @Transactional(readOnly = true)
    public GetMissionsResult getMissions(GetMissionsCommand command) {
        Long memberId = command.memberId();

        List<MemberMission> allMissions = memberMissionRepository.findAllWithMissionByMemberId(memberId);

        return GetMissionsResult.of(allMissions);
    }

    @Transactional
    public DoMissionResult doMission(DoMissionCommand command) {
        MemberMission memberMission = memberMissionRepository.findByIdFetchMissionAndMember(command.memberMissionId())
                .orElseThrow(() -> new GgumtleException(MissionErrorCode.NOT_FOUND_MEMBER_MISSION));

        if (!memberMission.getMember().getId().equals(command.memberId())) {
            throw new GgumtleException(MissionErrorCode.NOT_MISSION_OWNER);
        }

        memberMission.doMission();

        return DoMissionResult.from(memberMission);
    }

    @Transactional
    public GetRewardResult getReward(GetRewardCommand command) {
        MemberMission memberMission = memberMissionRepository.findByIdFetchMissionAndMember(command.memberMissionId())
                .orElseThrow(() -> new GgumtleException(MissionErrorCode.NOT_FOUND_MEMBER_MISSION));

        if (!memberMission.getMember().getId().equals(command.memberId())) {
            throw new GgumtleException(MissionErrorCode.NOT_MISSION_OWNER);
        }

        if (memberMission.getState() != MissionState.SUCCESS) {
            throw new GgumtleException(MissionErrorCode.CONFLICT_MISSION_STATE_FOR_REWARD);
        }

        memberMission.getReward();

        return GetRewardResult.from(memberMission);
    }
}
