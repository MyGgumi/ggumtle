package com.ggumtle.ggumtle.mission.application;

import com.ggumtle.ggumtle.member.domain.Member;
import com.ggumtle.ggumtle.mission.application.command.GetMissionsCommand;
import com.ggumtle.ggumtle.mission.application.result.GetMissionsResult;
import com.ggumtle.ggumtle.mission.domain.Mission;
import com.ggumtle.ggumtle.mission.domain.MemberMission;
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
            missions.add(MemberMission.createMemberMission(newMember, oneMission));
        }
        memberMissionRepository.saveAll(missions);
    }

    @Transactional(readOnly = true)
    public GetMissionsResult getMissions(GetMissionsCommand command){
        Long memberId = command.memberId();

        List<MemberMission> allMissions = memberMissionRepository.findAllWithMissionByMemberId(memberId);

        List<GetMissionsResult.Mission> missions = allMissions.stream()
                .map(memberMission -> new GetMissionsResult.Mission(
                        memberMission.getId(),
                        memberMission.getMission().getMissionName(),
                        memberMission.getMission().getMissionDescription(),
                        memberMission.getMission().getCount(),
                        memberMission.getCount(),
                        memberMission.getCompleteState().name()
                ))
                .toList();

        return GetMissionsResult.of(missions);
    }
}
