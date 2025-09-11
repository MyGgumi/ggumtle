package com.ggumtle.ggumtle.friend.application.result;

public record RejectFriendRequestResult(
        Long friendId,
        Long followerId,
        String nickname
) {
    public static RejectFriendRequestResult of(Long friendId, Long followerId, String nickname) {
        return new RejectFriendRequestResult(friendId,followerId, nickname);
    }
}
