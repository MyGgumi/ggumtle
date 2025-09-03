package com.ggumtle.ggumtle.friend.presentation.request;

import com.ggumtle.ggumtle.friend.application.command.RequestFriendCommand;

public record RequestFriendRequest(
        Long targetMemberId
) {
    public RequestFriendCommand toCommand(Long requesterId){
        return new RequestFriendCommand(requesterId, targetMemberId);
    }
}
