package com.example.domain.websocket.event

import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow

interface EventBus {
    suspend fun emit(event: WebSocketEvent)
    fun subscribe(): Flow<WebSocketEvent>
}
