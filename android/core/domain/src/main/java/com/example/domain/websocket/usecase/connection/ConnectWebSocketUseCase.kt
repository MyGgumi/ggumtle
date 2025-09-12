package com.ggumtle.domain.websocket.usecase.connection

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class ConnectWebSocketUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke() {
        webSocketRepository.connect()
    }
}