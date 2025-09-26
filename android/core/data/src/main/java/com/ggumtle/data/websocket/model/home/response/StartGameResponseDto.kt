package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.DreamServer
import com.ggumtle.domain.websocket.model.DreamStatus
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class StartGameResponseDto(
    val status: DreamStatus,
    val roomId: Long?,
    val dreamServer: DreamServerDto?
)

@Serializable
data class DreamServerDto(
    val host: String,
    val port: Int
)

fun StartGameResponseDto.toEvent(): WebSocketEvent.StartGameSuccess {
    return WebSocketEvent.StartGameSuccess(
        status = status,
        roomId = roomId,
        dreamServer = dreamServer?.let { DreamServer(it.host, it.port) }
    )
}