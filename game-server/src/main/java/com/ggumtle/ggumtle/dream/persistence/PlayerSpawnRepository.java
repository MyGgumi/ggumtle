package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.PlayerSpawn;
import org.springframework.data.jpa.repository.JpaRepository;

public interface PlayerSpawnRepository extends JpaRepository<PlayerSpawn, Integer> {
}
