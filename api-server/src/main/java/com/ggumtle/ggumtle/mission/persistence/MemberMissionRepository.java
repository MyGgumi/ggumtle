package com.ggumtle.ggumtle.mission.persistence;

import com.ggumtle.ggumtle.mission.domain.MemberMission;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MemberMissionRepository extends JpaRepository<MemberMission, Long> {
}
