package com.example.domain.websocket.usecase.home

import com.example.domain.websocket.event.EventBus
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import javax.inject.Inject

class ObserveLeavePartyUseCase @Inject constructor(
    private val eventBus: EventBus
) {
    operator fun invoke(): Flow<WebSocketEvent.LeavePartySuccess> {
        return eventBus.subscribe()
            .filter { event -> event is WebSocketEvent.LeavePartySuccess }
            .map { event -> event as WebSocketEvent.LeavePartySuccess }
    }
}