package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface FieldItemSpawnRepository extends JpaRepository<FieldItemSpawn, Integer> {
}
