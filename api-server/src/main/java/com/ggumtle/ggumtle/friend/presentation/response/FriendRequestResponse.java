package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.FriendRequestResult;

public record FriendRequestResponse(
        Long requesterId,
        Long targetMemberId
) {
    public static FriendRequestResponse from(FriendRequestResult result){
        return new FriendRequestResponse(result.requesterId(), result.targetMemberId());
    }
}
