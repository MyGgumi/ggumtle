package com.ggumtle.ggumtle.friend.application.result;

import com.ggumtle.ggumtle.friend.persistence.po.MemberPo;
import org.springframework.data.domain.Slice;
import java.util.List;

public record SearchMemberResult(
        boolean hasNext,
        List<Member> members
) {
    public enum RelationStatus { NONE, PENDING, ACCEPTED, REJECTED }

    public record Member(
            Long memberId,
            String nickname,
            RelationStatus status
    ) {
        public static Member fromPo(MemberPo po) {
            return new Member(
                    po.memberId(),
                    po.nickname(),
                    po.status() == null ? RelationStatus.NONE : RelationStatus.valueOf(po.status().name())
            );
        }
    }

    public static SearchMemberResult fromPoSlice(Slice<MemberPo> poSlice) {
        List<Member> members = poSlice.getContent().stream()
                .map(Member::fromPo)
                .toList();
        return new SearchMemberResult(poSlice.hasNext(), members);
    }
}

