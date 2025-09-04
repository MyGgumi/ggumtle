package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface GgumtleSpawnRepository extends JpaRepository<GgumtleSpawn, Integer> {
}
