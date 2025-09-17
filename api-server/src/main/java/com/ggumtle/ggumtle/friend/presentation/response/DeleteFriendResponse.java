package com.ggumtle.ggumtle.friend.presentation.response;

public record DeleteFriendResponse(
        Long followerid,
        String followerNickname,
        Long followeeId,
        String followeeNickname
) {
}
