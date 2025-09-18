package com.ggumtle.ggumtle.mission.application;

import com.ggumtle.ggumtle.member.domain.Member;
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
}
