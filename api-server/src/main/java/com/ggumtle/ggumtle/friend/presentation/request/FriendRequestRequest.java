package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.FriendRequestCommand;
import jakarta.validation.constraints.NotNull;

public record FriendRequestRequest(
        Long targetMemberId
) {
    public FriendRequestCommand toCommand(Long requesterId){
        return new FriendRequestCommand(requesterId, targetMemberId);
    }
}
