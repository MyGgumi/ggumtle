package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class ChangeMonggingTypeResponseDto(
    val memberId: Long,
    val classId: Long,
    val level: Long
)

fun ChangeMonggingTypeResponseDto.toEvent(): WebSocketEvent.ChangeMonggingTypeSuccess? {
    return WebSocketEvent.ChangeMonggingTypeSuccess(
        memberId = memberId,
        classId = classId,
        level = level,
    )
}