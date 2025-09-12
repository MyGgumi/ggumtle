package com.ggumtle.data.websocket.remote.datasource

import com.ggumtle.network.websocket.model.WebSocketConnectionState
import com.ggumtle.network.websocket.model.WebSocketReceiveMessageDto
import com.ggumtle.network.websocket.model.WebSocketSendMessageDto
import kotlinx.coroutines.flow.Flow

interface WebSocketRemoteDataSource {
    val connectionState: Flow<WebSocketConnectionState>
    val messages: Flow<WebSocketReceiveMessageDto>

    suspend fun connect()
    suspend fun disconnect()
    suspend fun sendMessage(message: WebSocketSendMessageDto)
}