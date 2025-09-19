package com.ggumtle.ggumtle.mission.persistence;

import com.ggumtle.ggumtle.mission.domain.MemberMission;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface MemberMissionRepository extends JpaRepository<MemberMission, Long> {
    @Query("""
        select mm
        from MemberMission mm
        join fetch mm.member
        join fetch mm.mission
        where mm.id = :id
        """)
    Optional<MemberMission> findByIdFetchMissionAndMember(Long id);

    @Query("""
           select mm
           from MemberMission mm
           join fetch mm.mission ms
           where mm.member.id = :memberId
           order by mm.id asc
           """)
    List<MemberMission> findAllWithMissionByMemberId(@Param("memberId") Long memberId);

    @Modifying(flushAutomatically = true, clearAutomatically = true)
    @Query("UPDATE MemberMission mm SET mm.doneCount = 0, mm.state = 'BEFORE_SUCCESS'")
    int initializeMemberMission();
}
