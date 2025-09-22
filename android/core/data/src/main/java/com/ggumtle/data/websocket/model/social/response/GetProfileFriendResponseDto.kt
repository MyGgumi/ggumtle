package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.UnityMonggingClass
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetProfileFriendResponseDto(
    val memberId: Long,
    val nickname: String,
    val unityMonggingClass: UnityMonggingClass,
    val monggingLevel: Int,
)

fun GetProfileFriendResponseDto.toEvent(): WebSocketEvent.GetProfileFriendSuccess? {
    return WebSocketEvent.GetProfileFriendSuccess(
        memberId = memberId,
        nickname = nickname,
        unityMonggingClass = unityMonggingClass,
        monggingLevel = monggingLevel,
    )
}