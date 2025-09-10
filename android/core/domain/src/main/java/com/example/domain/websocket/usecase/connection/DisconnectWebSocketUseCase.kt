package com.example.domain.websocket.usecase.connection

import com.example.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class DisconnectWebSocketUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke() {
        webSocketRepository.disconnect()
    }
}