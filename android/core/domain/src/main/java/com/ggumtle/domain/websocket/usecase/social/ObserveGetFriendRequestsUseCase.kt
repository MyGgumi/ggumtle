package com.ggumtle.domain.websocket.usecase.social

import com.ggumtle.domain.websocket.event.EventBus
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import javax.inject.Inject

class ObserveGetFriendRequestsUseCase @Inject constructor(
    private val eventBus: EventBus
) {
    operator fun invoke(): Flow<WebSocketEvent.GetFriendRequestsSuccess> {
        return eventBus.subscribe()
            .filter { event -> event is WebSocketEvent.GetFriendRequestsSuccess }
            .map { event -> event as WebSocketEvent.GetFriendRequestsSuccess }
    }
}