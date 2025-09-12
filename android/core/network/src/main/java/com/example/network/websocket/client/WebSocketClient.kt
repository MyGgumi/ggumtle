package com.ggumtle.network.websocket.client

import com.ggumtle.network.websocket.model.WebSocketConnectionState
import com.ggumtle.network.websocket.model.WebSocketReceiveMessageDto
import com.ggumtle.network.websocket.model.WebSocketSendMessageDto
import kotlinx.coroutines.flow.Flow

interface WebSocketClient {
    val connectionState: Flow<WebSocketConnectionState>
    val messages: Flow<WebSocketReceiveMessageDto>

    suspend fun connect()
    suspend fun disconnect()
    suspend fun sendMessage(message: WebSocketSendMessageDto)
}