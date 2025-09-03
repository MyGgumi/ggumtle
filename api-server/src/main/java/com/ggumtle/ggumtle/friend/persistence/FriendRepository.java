package com.ggumtle.ggumtle.friend.persistence;

import com.ggumtle.ggumtle.friend.domain.Friend;
import com.ggumtle.ggumtle.friend.domain.Status;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

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

    Optional<Friend> findByIdAndFollowee_Id(Long id, Long followeeId);
}
