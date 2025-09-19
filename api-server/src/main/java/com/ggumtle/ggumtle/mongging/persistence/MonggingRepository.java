package com.ggumtle.ggumtle.mongging.persistence;

import com.ggumtle.ggumtle.mongging.domain.Mongging;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;

public interface MonggingRepository extends JpaRepository<Mongging, Long> {
    @Query("""
        select m
        from Mongging m
        join fetch m.monggingClass
        join fetch m.owner
        where m.id = :monggingId
        """)
    Optional<Mongging> findByIdFetchClassAndOwner(Long monggingId);

    @Query("""
        select m
        from Mongging m
        join fetch m.monggingClass
        join fetch m.owner
        where m.owner.id = :ownerId and m.monggingClass.id = :classId
        """)
    Optional<Mongging> findByOwnerIdAndClassIdFetchClassAndOwner(Long ownerId, Long classId);

    @Query("""
        select m
        from Mongging m
        join fetch m.monggingClass
        join fetch m.owner
        where m.id in :ids
        """)
    List<Mongging> findAllByIdInFetchClassAndOwner(List<Long> ids);

    @Query("""
        select m
        from Mongging m
        join fetch m.monggingClass
        join fetch m.owner
        where m.owner.id = :ownerId
        """)
    List<Mongging> findAllByOwnerIdFetchClassAndOwner(Long ownerId);
}
