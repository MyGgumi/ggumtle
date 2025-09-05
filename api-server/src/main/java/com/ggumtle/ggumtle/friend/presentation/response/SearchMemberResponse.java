package com.ggumtle.ggumtle.friend.presentation.response;

import com.ggumtle.ggumtle.friend.application.result.SearchMemberResult;

import java.util.List;

public record SearchMemberResponse(
        boolean hasNext,
        List<Member> members
) {
    public enum RelationStatus { NONE, PENDING, ACCEPTED, REJECTED }

    public record Member(
            Long memberId,
            String nickname,
            RelationStatus status
    ) {}
    public static SearchMemberResponse from(SearchMemberResult result) {
        List<Member> list = result.members().stream()
                .map(m -> new Member(
                        m.memberId(),
                        m.nickname(),
                        RelationStatus.valueOf(m.status().name())
                ))
                .toList();

        return new SearchMemberResponse(result.hasNext(), list);
    }
}
