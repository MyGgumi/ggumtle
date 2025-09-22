package com.ggumtle.ggumtle.member.persistence;

import com.ggumtle.ggumtle.member.domain.Member;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface MemberRepository extends JpaRepository<Member, Long> {
    Optional<Member> findByGoogleEmail(String googleEmail);

    boolean existsByNickname(String nickname);

    List<Member> findAllByIdIn(List<Long> ids);

    Optional<Member> findByNickname(String nickname);
}
