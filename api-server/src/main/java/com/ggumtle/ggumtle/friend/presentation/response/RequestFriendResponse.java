package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.RequestFriendsResult;

public record RequestFriendResponse(
        Long requesterId,
        Long targetMemberId
) {
    public static RequestFriendResponse from(RequestFriendsResult result){
        return new RequestFriendResponse(result.requesterId(), result.targetMemberId());
    }
}
