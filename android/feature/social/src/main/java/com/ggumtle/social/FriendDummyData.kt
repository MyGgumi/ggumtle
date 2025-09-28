package com.ggumtle.social

import com.ggumtle.domain.model.MemberConnectionState
import com.ggumtle.domain.websocket.model.Friend
import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.domain.websocket.model.SentRequest

object FriendDummyData {

    fun getFriendsList(): List<Friend> {
        return listOf(
            Friend(
                id = 1L,
                nickname = "꿈틀왕",
                connectionState = MemberConnectionState.ONLINE
            ),
            Friend(
                id = 2L,
                nickname = "젤리헌터",
                connectionState = MemberConnectionState.OFFLINE
            ),
            Friend(
                id = 3L,
                nickname = "무지개빛",
                connectionState = MemberConnectionState.INGAME
            ),
            Friend(
                id = 4L,
                nickname = "스타더스트",
                connectionState = MemberConnectionState.ONLINE
            ),
            Friend(
                id = 5L,
                nickname = "몽글몽글",
                connectionState = MemberConnectionState.OFFLINE
            ),
        )
    }

    fun getReceivedFriendRequests(): List<FriendRequest> {
        return listOf(
            FriendRequest(
                friendRequestId = 101L,
                memberId = 21L,
                nickname = "하늘방울"
            ),
            FriendRequest(
                friendRequestId = 102L,
                memberId = 22L,
                nickname = "미라클킹"
            ),
        )
    }

    fun getSentFriendRequests(): List<SentRequest> {
        return listOf(
            SentRequest(
                id = 201L,
                toUserId = 31L,
                toUserName = "세진스타"
            ),
            SentRequest(
                id = 202L,
                toUserId = 32L,
                toUserName = "민서공주"
            ),
        )
    }
}