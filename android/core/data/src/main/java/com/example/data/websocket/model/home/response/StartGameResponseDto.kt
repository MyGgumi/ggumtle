package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.Dream
import com.example.domain.websocket.model.DreamStatus
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class StartGameResponseDto(
    val status: DreamStatus,
    val dream: DreamDto?
)

@Serializable
data class DreamDto(
    val roomId: Long,
    val dreamServerId: String
)

fun StartGameResponseDto.toEvent(): WebSocketEvent.StartGameSuccess {
    return WebSocketEvent.StartGameSuccess(
        status = status,
        dream = dream?.let { Dream(dream.roomId, it.dreamServerId) }
    )
}