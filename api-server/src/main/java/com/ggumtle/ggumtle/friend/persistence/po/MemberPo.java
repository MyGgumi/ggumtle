package com.ggumtle.ggumtle.friend.persistence.po;

import com.ggumtle.ggumtle.friend.domain.Status;

public record MemberPo(
        Long memberId,
        String nickname,
        Status status
) {
}
