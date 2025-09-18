package com.ggumtle.ggumtle.common.event;

import java.util.List;

public record MemberDreamOutEvent(
        List<Long> memberIds
) {
}
