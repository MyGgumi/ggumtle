package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.EnhanceStat;
import com.ggumtle.ggumtle.mongging.domain.MonggingClass;
import com.ggumtle.ggumtle.mongging.persistence.po.MonggingStatPo;
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

    @Query(value = """
        SELECT new com.ggumtle.ggumtle.mongging.persistence.po.MonggingStatPo(
                mongging.owner.id,
                mongging.owner.nickname,
                mongging.id,
                mongging.level,
                es.monggingClass.id,
                CASE
                    WHEN es.monggingClass.id = 1 THEN es.monggingClass.baseHeal * es.enhancePercentage
                    WHEN es.monggingClass.id = 2 THEN es.monggingClass.baseHealth * es.enhancePercentage
                    WHEN es.monggingClass.id = 3THEN es.monggingClass.baseWork * es.enhancePercentage
                    ELSE 0.0
                END as additional_stat
        )
        FROM EnhanceStat es
        JOIN es.monggingClass
        JOIN Mongging mongging ON mongging.monggingClass.id = es.monggingClass.id
        JOIN mongging.owner
        WHERE mongging.id IN :monggingIds
        AND es.level = mongging.level
    """)
    List<MonggingStatPo> findAllByMonggingIds(List<Long> monggingIds);
}
