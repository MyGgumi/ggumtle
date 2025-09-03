package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.GetFriendRequestsResult;

import java.util.List;

public record GetFriendRequestsResponse(
        List<Friend> friendRequests
) {
    public static GetFriendRequestsResponse from(GetFriendRequestsResult result) {
        return new GetFriendRequestsResponse(
                result.friendRequests().stream()
                        .map(f -> new Friend(f.memberId(), f.nickname()))
                        .toList()
        );
    }

    public record Friend(
            Long memberId,
            String nickname
    ) {}
}