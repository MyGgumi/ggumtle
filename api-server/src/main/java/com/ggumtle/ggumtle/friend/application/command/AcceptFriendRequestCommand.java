package com.ggumtle.ggumtle.friend.application.command;

public record AcceptFriendRequestCommand(
        Long loginMemberId,
        Long friendId
) {
}
