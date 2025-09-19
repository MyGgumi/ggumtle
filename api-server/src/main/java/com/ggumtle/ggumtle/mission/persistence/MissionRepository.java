package com.ggumtle.ggumtle.mission.persistence;

import com.ggumtle.ggumtle.mission.domain.Mission;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface MissionRepository extends JpaRepository<Mission, Long> {
}
