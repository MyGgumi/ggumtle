package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.DeleteFriendCommand;

public record DeleteFriendRequest(
        Long friendId
) {
    public DeleteFriendCommand toCommand(Long requesterId){
        return new DeleteFriendCommand(requesterId, friendId);
    }
}
