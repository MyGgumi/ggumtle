package com.example.data.websocket.model.social.response

import com.example.domain.websocket.model.MonggingClass
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetProfileFriendResponseDto(
    val memberId: Long,
    val nickname: String,
    val monggingClass: MonggingClass,
    val monggingLevel: Int,
)

fun GetProfileFriendResponseDto.toEvent(): WebSocketEvent.GetProfileFriendSuccess? {
    return WebSocketEvent.GetProfileFriendSuccess(
        memberId = memberId,
        nickname = nickname,
        monggingClass = monggingClass,
        monggingLevel = monggingLevel,
    )
}