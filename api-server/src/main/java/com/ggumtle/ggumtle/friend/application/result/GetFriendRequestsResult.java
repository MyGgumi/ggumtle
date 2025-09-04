package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.friend.domain.Friend;
import com.ggumtle.ggumtle.member.domain.Member;

import java.util.List;

public record GetFriendRequestsResult(
        List<FriendItem> friendRequests
) {
    public static GetFriendRequestsResult of(List<Friend> friendRequests) {
        return new GetFriendRequestsResult(
                friendRequests == null ? List.of() :
                        friendRequests.stream()
                                .map(f -> new FriendItem(f.getId(), f.getFollower().getId(),f.getFollower().getNickname()))
                                .toList()
        );
    }

    public record FriendItem(
            Long id,
            Long memberId,
            String nickname
    ) {}
}