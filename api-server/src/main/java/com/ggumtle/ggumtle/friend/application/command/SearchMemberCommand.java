package com.ggumtle.ggumtle.friend.application.command;

public record SearchMemberCommand(
        Long requesterId,
        String keyword,
        int page,
        int size
) {
}
