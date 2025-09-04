package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.RejectFriendRequestCommand;

public record RejectFriendRequestRequest(
        Long friendId
) {
    public RejectFriendRequestCommand toCommand(Long loginMemberId) {
        return new RejectFriendRequestCommand(loginMemberId, friendId);
    }
}
