package com.example.domain.websocket.model

import com.example.domain.model.Member

sealed class WebSocketEvent {
    // 연결 상태 이벤트
    data object Connected : WebSocketEvent()
    data object Disconnected : WebSocketEvent()
    data object Connecting : WebSocketEvent()
    data class Failed(val error: Throwable?) : WebSocketEvent()

    data class MemberSearchSuccess(
        val hasNext: Boolean,
        val members: List<Member>
    ) : WebSocketEvent()

    data class RequestFriendSuccess(
        val friendId: Long,
        val followerId: Long,
        val followerNickname: String,
        val followeeId: Long,
        val followeeNickname: String
        ) : WebSocketEvent()

    data class GetFriendRequestsSuccess(
        val friendRequests: List<FriendRequest>
    ) : WebSocketEvent()

    data class GetFriendsSuccess(
        val friends: List<Friend>
    ) : WebSocketEvent()

    data class AcceptFriendRequestSuccess(
        val followerId: Long,
        val followerNickname: String,
        val followeeId: Long,
        val followeeNickname: String
    ) : WebSocketEvent()

    data class RejectFriendRequestSuccess(
        val rejectedId: Long,
        val nickname: String
    ) : WebSocketEvent()
}