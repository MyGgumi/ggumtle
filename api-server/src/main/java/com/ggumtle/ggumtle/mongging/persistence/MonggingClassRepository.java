package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MonggingClassRepository extends JpaRepository<MonggingClass, Long> {

}
