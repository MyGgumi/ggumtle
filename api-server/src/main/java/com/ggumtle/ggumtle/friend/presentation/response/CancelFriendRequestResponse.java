package com.ggumtle.ggumtle.friend.presentation.response;

public record CancelFriendRequestResponse(
        Long friendId,
        Long followerId,
        String followerNickname,
        Long followeeId,
        String followeeNickname
) {
}
