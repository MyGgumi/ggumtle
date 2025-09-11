package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.member.domain.Member;

import java.util.List;

public record GetFriendsResult(
        List<Friend> friends
) {

    public record Friend(
            Long memberId,
            String nickname,
            String memberState
    ) {}
}
