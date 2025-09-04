package com.ggumtle.ggumtle.friend.application.result;

public record AcceptFriendRequestResult(
        Long followerId,
        String nickname
) {
    public static AcceptFriendRequestResult of(Long followerId, String nickname) {
        return new AcceptFriendRequestResult(followerId, nickname);
    }
}
