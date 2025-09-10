package com.ggumtle.ggumtle.friend.application.result;

public record RequestFriendsResult(
        Long friendId,
        Long requesterId,
        String requesterNickname,
        Long targetMemberId,
        String targetMemberNickname
) {
    public static RequestFriendsResult of(Long friendId, Long requesterId, String requesterNickname ,Long targetMemberId, String targetMemberNickname) {
        return new RequestFriendsResult(
                friendId,
                requesterId,
                requesterNickname,
                targetMemberId,
                targetMemberNickname);
    }
}
