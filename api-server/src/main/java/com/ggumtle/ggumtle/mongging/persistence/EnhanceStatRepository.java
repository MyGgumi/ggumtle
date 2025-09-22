package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;

@Repository
public interface EnhanceStatRepository extends JpaRepository<EnhanceStat, Long> {
    
    @Query(value = """
        SELECT es
        FROM EnhanceStat es
        WHERE es.monggingClass = :monggingClass AND es.level >= :level
        ORDER BY es.level ASC
        LIMIT 2
    """)
    List<EnhanceStat> findTop2ByMonggingClassAndLevel(MonggingClass monggingClass, Integer level);
}
