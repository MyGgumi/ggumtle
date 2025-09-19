package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.EnhancePercentage;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.Optional;

@Repository
public interface EnhancePercentageRepository extends JpaRepository<EnhancePercentage, Long> {

    Optional<EnhancePercentage> findByMonggingClassAndLevel(MonggingClass monggingClass, int level);
}
