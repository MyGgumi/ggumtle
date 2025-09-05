package com.ggumtle.ggumtle.friend.persistence;

import com.ggumtle.ggumtle.friend.domain.Friend;
import com.ggumtle.ggumtle.friend.domain.Status;
import com.ggumtle.ggumtle.friend.persistence.po.MemberPo;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Slice;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

import java.util.List;
import java.util.Optional;

public interface FriendRepository extends JpaRepository<Friend, Long> {
    boolean existsByFollower_IdAndFollowee_IdAndStatus(Long followerId, Long followeeId, Status status);

    @Query("""
           SELECT (count(f) > 0)
           FROM Friend f
           WHERE ((f.follower.id = :followerId and f.followee.id = :followeeId)
              OR  (f.follower.id = :followeeId and f.followee.id = :followerId))
              AND f.status = "ACCEPT"
           """)
    boolean areFriends(Long followerId, Long followeeId);
    @EntityGraph(attributePaths = "followee")
    List<Friend> findAllByFollower_IdAndStatus(Long followerId, Status status);

    @EntityGraph(attributePaths = "follower")
    List<Friend> findAllByFollowee_IdAndStatus(Long followeeId, Status status);

    @EntityGraph(attributePaths = "follower")
    Optional<Friend> findByIdAndFollowee_Id(Long id, Long followeeId);

    @Query("""
        select new com.ggumtle.ggumtle.friend.persistence.MemberPo(
            m.id,
            m.nickname,
            f.status
        )
        from Member m
        left join Friend f
          on (
               (f.follower.id = :myId and f.followee.id = m.id)
            or (f.followee.id = :myId and f.follower.id = m.id)
          )
        where m.id <> :myId
          and (:keyword is null or :keyword = '' or m.nickname like concat(:keyword, '%'))
        order by m.nickname
        """)
    Slice<MemberPo> searchMembers(
            @Param("myId") Long myId,
            @Param("keyword") String keyword,
            Pageable pageable
    );

}
