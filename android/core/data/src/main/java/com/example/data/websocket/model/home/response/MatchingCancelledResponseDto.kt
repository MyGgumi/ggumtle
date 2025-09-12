package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class MatchingCancelledResponseDto(
    val message: String,
)

fun MatchingCancelledResponseDto.toEvent(): WebSocketEvent.MatchingCancelledSuccess? {
    return WebSocketEvent.MatchingCancelledSuccess(
        message = message,
    )
}