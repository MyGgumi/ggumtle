package com.ggumtle.ggumtle.mission.application;

import com.ggumtle.ggumtle.mission.persistence.MemberMissionRepository;
import jakarta.transaction.Transactional;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MissionInitializer {
    private final MemberMissionRepository memberMissionRepository;

    @Scheduled(cron = "0 0 0 * * *")
    @Transactional
    public void initializeDailyMissions() {
        log.info("미션 초기화 시작");

        int count = memberMissionRepository.initializeMemberMission();

        log.info("미션 초기화 완료: {}행 초기화", count);
    }
}
