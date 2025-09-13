package com.ggumtle.ggumtle.friend.application.command;

public record DeleteFriendCommand(
        Long requesterId,
        Long friendId
) {
}
