package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.GetFriendRequestsResult;

import java.util.List;

public record GetFriendRequestsResponse(
        List<Friend> friendRequests
) {
    public static GetFriendRequestsResponse from(GetFriendRequestsResult result) {
        return new GetFriendRequestsResponse(
                result.friendRequests().stream()
                        .map(f -> new Friend(f.id(),f.memberId(), f.nickname()))
                        .toList()
        );
    }

    public record Friend(
            Long id,
            Long memberId,
            String nickname
    ) {}
}