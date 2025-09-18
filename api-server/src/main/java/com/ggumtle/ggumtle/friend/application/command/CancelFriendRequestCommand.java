package com.ggumtle.ggumtle.friend.application.command;

public record CancelFriendRequestCommand(
        Long requesterId,
        Long friendId
) {
}
