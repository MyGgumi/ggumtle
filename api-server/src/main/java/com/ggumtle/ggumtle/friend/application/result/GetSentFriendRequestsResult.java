package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.friend.domain.Friend;

import java.util.List;

public record GetSentFriendRequestsResult(
        List<FriendItem> friendRequests
) {
    public static GetSentFriendRequestsResult of(List<Friend> friendRequests) {
        return new GetSentFriendRequestsResult(
                friendRequests == null ? List.of() :
                        friendRequests.stream()
                                .map(f -> new FriendItem(f.getId(), f.getFollowee().getId(),f.getFollowee().getNickname()))
                                .toList()
        );
    }

    public record FriendItem(
            Long friendId,
            Long memberId,
            String nickname
    ) {}
}