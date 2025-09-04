package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface BoxSpawnRepository extends JpaRepository<BoxSpawn, Integer> {
}
