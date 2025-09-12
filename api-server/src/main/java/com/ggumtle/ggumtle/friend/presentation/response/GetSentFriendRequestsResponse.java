package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.GetSentFriendRequestsResult;

import java.util.List;

public record GetSentFriendRequestsResponse(
        List<Friend> friendRequests
) {
    public static GetSentFriendRequestsResponse from(GetSentFriendRequestsResult result) {
        return new GetSentFriendRequestsResponse(
                result.friendRequests().stream()
                        .map(f -> new Friend(f.friendId(),f.memberId(), f.nickname()))
                        .toList()
        );
    }

    public record Friend(
            Long friendId,
            Long memberId,
            String nickname
    ) {}
}