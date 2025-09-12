package com.ggumtle.domain.websocket.event

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow

interface EventBus {
    suspend fun emit(event: WebSocketEvent)
    fun subscribe(): Flow<WebSocketEvent>
}
