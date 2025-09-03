package com.ggumtle.ggumtle.friend.application.result;

public record FriendRequestResult(
        Long requesterId,
        Long targetMemberId
) {
    public static FriendRequestResult of(Long requesterId, Long targetMemberId) {
        return new FriendRequestResult(requesterId, targetMemberId);
    }
}
