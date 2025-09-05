package com.example.network.websocket.datasource

import com.example.network.websocket.model.WebSocketState
import kotlinx.coroutines.flow.Flow

interface WebSocketDataSource {
    fun connect(url: String?): Flow<WebSocketState>
    fun disconnect()
    fun sendMessage(message: String)
    fun observeMessages(): Flow<String>
    fun observeConnectionState(): Flow<WebSocketState>
}