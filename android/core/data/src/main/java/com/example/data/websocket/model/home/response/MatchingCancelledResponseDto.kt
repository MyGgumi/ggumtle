package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.WebSocketEvent
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