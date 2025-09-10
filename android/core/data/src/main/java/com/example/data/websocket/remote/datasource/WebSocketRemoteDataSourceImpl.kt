package com.example.data.websocket.remote.datasource

import com.example.network.websocket.client.WebSocketClient
import com.example.network.websocket.model.WebSocketConnectionState
import com.example.network.websocket.model.WebSocketReceiveMessageDto
import com.example.network.websocket.model.WebSocketSendMessageDto
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class WebSocketRemoteDataSourceImpl @Inject constructor(
    private val webSocketClient: WebSocketClient
) : WebSocketRemoteDataSource {

    override val connectionState: Flow<WebSocketConnectionState> = webSocketClient.connectionState
    override val messages: Flow<WebSocketReceiveMessageDto> = webSocketClient.messages

    override suspend fun connect() {
        webSocketClient.connect()
    }

    override suspend fun disconnect() {
        webSocketClient.disconnect()
    }

    override suspend fun sendMessage(message: WebSocketSendMessageDto) {
        webSocketClient.sendMessage(message)
    }
}