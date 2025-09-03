package com.ggumtle.ggumtle.friend.application.command;

public record RequestFriendCommand(
        Long requesterId,
        Long targetMemberId
) {
}
