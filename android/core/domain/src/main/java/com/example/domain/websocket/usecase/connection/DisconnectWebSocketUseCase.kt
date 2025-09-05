package com.example.domain.websocket.usecase.connection

import com.example.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class DisconnectWebSocketUseCase @Inject constructor(
    private val repository: WebSocketRepository
) {
    operator fun invoke() = repository.disconnect()
}