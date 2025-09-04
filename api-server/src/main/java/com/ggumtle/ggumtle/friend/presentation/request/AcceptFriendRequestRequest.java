package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.AcceptFriendRequestCommand;

public record AcceptFriendRequestRequest(
        Long friendId
) {
    public AcceptFriendRequestCommand toCommand(Long loginMemberId) {
        return new AcceptFriendRequestCommand(loginMemberId, friendId);
    }
}
