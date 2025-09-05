package com.example.domain.websocket.usecase.connection

import com.example.common.network.websocket.WebSocketResult
import com.example.domain.websocket.model.ConnectionStatus
import com.example.domain.websocket.repository.WebSocketRepository
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ConnectWebSocketUseCase @Inject constructor(
    private val repository: WebSocketRepository
) {
    operator fun invoke(url: String?): Flow<WebSocketResult<ConnectionStatus>> =
        repository.connectToWebSocket(url)
}