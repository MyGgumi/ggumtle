package com.ggumtle.ggumtle.friend.application.result;

public record RequestFriendsResult(
        Long requesterId,
        Long targetMemberId
) {
    public static RequestFriendsResult of(Long requesterId, Long targetMemberId) {
        return new RequestFriendsResult(requesterId, targetMemberId);
    }
}
