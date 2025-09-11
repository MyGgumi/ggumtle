package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.GetFriendsResult;
import com.ggumtle.ggumtle.friend.domain.MemberState;

import java.util.List;

public record GetFriendsResponse(
        List<Friend> friends
) {
    public static GetFriendsResponse from(GetFriendsResult result) {
        return new GetFriendsResponse(
                result.friends().stream()
                        .map(f -> new Friend(f.memberId(), f.nickname(),f.memberState()))
                        .toList()
        );
    }

    public record Friend(
            Long memberId,
            String nickname,
            String memberState
    ) {}
}