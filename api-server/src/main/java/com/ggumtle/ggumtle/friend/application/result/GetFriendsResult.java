package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.member.domain.Member;

import java.util.List;

public record GetFriendsResult(
        List<Friend> friends
) {
    public static GetFriendsResult of(List<Member> members) {
        return new GetFriendsResult(
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
