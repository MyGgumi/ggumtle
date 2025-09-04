package com.ggumtle.ggumtle.friend.application.result;

public record RejectFriendRequestResult(
        Long followerId,
        String nickname
) {
    public static RejectFriendRequestResult of(Long followerId, String nickname) {
        return new RejectFriendRequestResult(followerId, nickname);
    }
}
