package com.ggumtle.ggumtle.friend.application.command;

public record RejectFriendRequestCommand(
        Long loginMemberId,
        Long friendId
) {
}
