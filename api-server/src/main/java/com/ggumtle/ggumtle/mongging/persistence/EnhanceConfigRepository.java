package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.EnhanceConfig;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface EnhanceConfigRepository extends JpaRepository<EnhanceConfig, Long> {

    Optional<EnhanceConfig> findByMonggingClassAndLevel(MonggingClass monggingClass, int level);
}
