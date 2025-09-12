package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class RejectFriendResponseDto(
    val rejectedId: Long,
    val nickname: String
)

fun RejectFriendResponseDto.toEvent(): WebSocketEvent.RejectFriendRequestSuccess? {
    return WebSocketEvent.RejectFriendRequestSuccess(
        rejectedId = rejectedId,
        nickname = nickname
    )
}