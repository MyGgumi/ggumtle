package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetFriendRequestsResponseDto(
    val friendRequests: List<FriendRequestsResponseDto>
)

@Serializable
data class FriendRequestsResponseDto(
    val id: Long,
    val memberId: Long,
    val nickname: String
)

fun GetFriendRequestsResponseDto.toEvent(): WebSocketEvent.GetFriendRequestsSuccess {
    return WebSocketEvent.GetFriendRequestsSuccess(
        friendRequests = this.friendRequests.map { friendResponse ->
            FriendRequest(
                friendRequestId = friendResponse.id,
                memberId = friendResponse.memberId,
                nickname = friendResponse.nickname
            )
        }
    )
}