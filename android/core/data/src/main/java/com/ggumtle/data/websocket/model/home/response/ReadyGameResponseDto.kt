package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class ReadyGameResponseDto(
    val memberId: Long,
    val isReady: Boolean,
)

fun ReadyGameResponseDto.toEvent(): WebSocketEvent.ReadyGameSuccess? {
    return WebSocketEvent.ReadyGameSuccess(
        memberId = memberId,
        isReady = isReady
    )
}