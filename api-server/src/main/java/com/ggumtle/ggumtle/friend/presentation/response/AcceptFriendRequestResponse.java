package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.AcceptFriendRequestResult;

public record AcceptFriendRequestResponse(
        Long followerId,
        String nickname
) {
    public static AcceptFriendRequestResponse from(AcceptFriendRequestResult result) {
        return new AcceptFriendRequestResponse(result.followerId(), result.nickname());
    }
}
