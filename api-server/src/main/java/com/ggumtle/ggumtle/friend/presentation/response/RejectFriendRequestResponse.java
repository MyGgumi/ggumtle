package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.RejectFriendRequestResult;

public record RejectFriendRequestResponse(
        Long followerId,
        String nickname
) {
    public static RejectFriendRequestResponse from(RejectFriendRequestResult result) {
        return new RejectFriendRequestResponse(result.followerId(), result.nickname());
    }
}
