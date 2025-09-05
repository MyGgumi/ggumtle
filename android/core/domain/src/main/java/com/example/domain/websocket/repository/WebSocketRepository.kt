package com.example.domain.websocket.repository

import com.example.common.network.websocket.WebSocketResult
import com.example.domain.websocket.model.ConnectionStatus
import com.example.domain.websocket.model.response.WebSocketResponse
import kotlinx.coroutines.flow.Flow

interface WebSocketRepository {
    fun connectToWebSocket(url: String?): Flow<WebSocketResult<ConnectionStatus>>
    fun <T> sendMessage(type: String, data: T): Flow<WebSocketResult<Unit>>
    fun observeMessages(): Flow<WebSocketResult<WebSocketResponse<*>>>
    fun disconnect()
}