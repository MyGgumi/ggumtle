package com.example.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class CancelFriendRequestResponseDto(
    val friendId: Long,
    val requesterId: Long,
    val nickname: String,
)

fun CancelFriendRequestResponseDto.toEvent(): WebSocketEvent.CancelFriendRequestSuccess? {
    return WebSocketEvent.CancelFriendRequestSuccess(
        friendRequestId = friendId,
        requesterId = requesterId,
        nickname = nickname,
    )
}