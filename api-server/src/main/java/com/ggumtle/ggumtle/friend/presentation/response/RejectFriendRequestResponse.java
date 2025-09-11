package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.RejectFriendRequestResult;

public record RejectFriendRequestResponse(
        Long friendId,
        Long followerId,
        String nickname
) {
    public static RejectFriendRequestResponse from(RejectFriendRequestResult result) {
        return new RejectFriendRequestResponse(result.friendId(),result.followerId(), result.nickname());
    }
}
