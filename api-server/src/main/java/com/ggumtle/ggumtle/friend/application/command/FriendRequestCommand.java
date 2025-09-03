package com.ggumtle.ggumtle.friend.application.command;

public record FriendRequestCommand(
        Long requesterId,
        Long targetMemberId
) {
}
