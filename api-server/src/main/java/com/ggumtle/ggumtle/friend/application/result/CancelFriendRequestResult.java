package com.ggumtle.ggumtle.friend.application.result;

public record CancelFriendRequestResult(
        Long friendId,
        Long followerId,
        String followerNickname,
        Long followeeId,
        String followeeNickname
) {
}
