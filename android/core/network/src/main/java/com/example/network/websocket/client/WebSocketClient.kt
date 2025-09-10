package com.example.network.websocket.client

import com.example.network.websocket.model.WebSocketConnectionState
import com.example.network.websocket.model.WebSocketReceiveMessageDto
import com.example.network.websocket.model.WebSocketSendMessageDto
import kotlinx.coroutines.flow.Flow

interface WebSocketClient {
    val connectionState: Flow<WebSocketConnectionState>
    val messages: Flow<WebSocketReceiveMessageDto>

    suspend fun connect()
    suspend fun disconnect()
    suspend fun sendMessage(message: WebSocketSendMessageDto)
}