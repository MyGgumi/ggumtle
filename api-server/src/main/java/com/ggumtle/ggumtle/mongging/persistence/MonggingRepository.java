package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.Mongging;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MonggingRepository extends JpaRepository<Mongging, Long> {
}
