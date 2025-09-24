package com.ggumtle.ggumtle.dream.persistence;

import com.ggumtle.ggumtle.dream.domain.DreamServer;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface DreamServerRepository extends JpaRepository<DreamServer, Long> {
}
