package com.ggumtle.data.websocket.remote.datasource

import com.ggumtle.network.websocket.client.WebSocketClient
import com.ggumtle.network.websocket.model.WebSocketConnectionState
import com.ggumtle.network.websocket.model.WebSocketReceiveMessageDto
import com.ggumtle.network.websocket.model.WebSocketSendMessageDto
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