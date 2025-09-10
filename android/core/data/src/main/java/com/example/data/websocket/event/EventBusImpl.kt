package com.example.data.websocket.event

import android.util.Log
import com.example.domain.websocket.event.EventBus
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.*

@Singleton
class EventBusImpl @Inject constructor() : EventBus {

    private val _events = MutableSharedFlow<WebSocketEvent>(
        replay = 0,
        extraBufferCapacity = 256,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )

    override suspend fun emit(event: WebSocketEvent) {
        _events.tryEmit(event)
        Log.d("EventBus", "Event emitted: $event")
    }

    override fun subscribe(): Flow<WebSocketEvent> = _events.asSharedFlow()
}