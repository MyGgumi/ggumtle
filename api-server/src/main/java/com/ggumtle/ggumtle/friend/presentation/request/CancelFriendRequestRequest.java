package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.CancelFriendRequestCommand;

public record CancelFriendRequestRequest(
        Long friendId
) {
    public CancelFriendRequestCommand toCommand(Long requesterId){
        return new CancelFriendRequestCommand(requesterId, friendId);
    }
}
