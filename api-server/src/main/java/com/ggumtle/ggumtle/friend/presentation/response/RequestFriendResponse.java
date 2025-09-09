package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.RequestFriendsResult;

public record RequestFriendResponse(
        Long followerId,
        String followerNickname,
        Long followeeId,
        String followeeNickname
) {
    public static RequestFriendResponse from(RequestFriendsResult result){
        return new RequestFriendResponse(result.requesterId(), result.requesterNickname(), result.targetMemberId(),  result.targetMemberNickname());
    }
}
