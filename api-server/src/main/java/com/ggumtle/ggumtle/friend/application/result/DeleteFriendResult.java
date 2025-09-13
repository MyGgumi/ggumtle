package com.ggumtle.ggumtle.friend.application.result;

public record DeleteFriendResult(
        Long followerId,
        String followerNickname,
        Long followeeId,
        String follweeNickname
) {
}
