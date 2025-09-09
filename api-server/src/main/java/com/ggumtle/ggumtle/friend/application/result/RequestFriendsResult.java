package com.ggumtle.ggumtle.friend.application.result;

public record RequestFriendsResult(
        Long requesterId,
        String requesterNickname,
        Long targetMemberId,
        String targetMemberNickname
) {
    public static RequestFriendsResult of(Long requesterId, String requesterNickname ,Long targetMemberId, String targetMemberNickname) {
        return new RequestFriendsResult(requesterId, requesterNickname, targetMemberId, targetMemberNickname);
    }
}
