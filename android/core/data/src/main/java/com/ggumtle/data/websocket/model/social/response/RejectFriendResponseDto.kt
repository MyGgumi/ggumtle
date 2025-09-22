package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class RejectFriendResponseDto(
    val friendId: Long,
    val rejectedId: Long,
    val nickname: String
)

fun RejectFriendResponseDto.toEvent(): WebSocketEvent.RejectFriendRequestSuccess? {
    return WebSocketEvent.RejectFriendRequestSuccess(
        friendId = friendId,
        rejectedId = rejectedId,
        nickname = nickname
    )
}