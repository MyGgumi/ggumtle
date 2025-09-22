package com.ggumtle.domain.websocket.usecase.home

import com.ggumtle.domain.websocket.event.EventBus
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import javax.inject.Inject

class ObserveInvitePartyUseCase @Inject constructor(
    private val eventBus: EventBus
) {
    operator fun invoke(): Flow<WebSocketEvent.InvitePartySuccess> {
        return eventBus.subscribe()
            .filter { event -> event is WebSocketEvent.InvitePartySuccess }
            .map { event -> event as WebSocketEvent.InvitePartySuccess }
    }
}