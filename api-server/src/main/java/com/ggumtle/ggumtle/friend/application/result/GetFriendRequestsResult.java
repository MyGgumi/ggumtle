package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.member.domain.Member;

import java.util.List;

public record GetFriendRequestsResult(
        List<Friend> friendRequests
) {
    public static GetFriendRequestsResult of(List<Member> members) {
        return new GetFriendRequestsResult(
                members == null ? List.of() :
                        members.stream()
                                .map(m -> new Friend(m.getId(), m.getNickname()))
                                .toList()
        );
    }

    public record Friend(
            Long memberId,
            String nickname
    ) {}
}