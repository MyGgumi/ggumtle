package com.ggumtle.ggumtle.friend.application.result;

public record AcceptFriendRequestResult(
        Long followerId,
        String followerNickname,
        Long followeeId,
        String followeeNickname
) {
    public static AcceptFriendRequestResult of(Long followerId,String followerNickname, Long followeeId, String followeeNickname) {
        return new AcceptFriendRequestResult(followerId,followerNickname,followeeId, followeeNickname);
    }
}
