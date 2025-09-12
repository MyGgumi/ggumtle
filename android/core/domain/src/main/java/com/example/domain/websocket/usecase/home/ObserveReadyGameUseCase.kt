package com.example.domain.websocket.usecase.home

import com.example.domain.websocket.event.EventBus
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import javax.inject.Inject

class ObserveReadyGameUseCase @Inject constructor(
    private val eventBus: EventBus
) {
    operator fun invoke(): Flow<WebSocketEvent.ReadyGameSuccess> {
        return eventBus.subscribe()
            .filter { event -> event is WebSocketEvent.ReadyGameSuccess }
            .map { event -> event as WebSocketEvent.ReadyGameSuccess }
    }
}