package com.example.domain.websocket.model

import com.example.domain.websocket.model.response.WebSocketResponse

sealed class ConnectionStatus {
    object Connected : ConnectionStatus()
    object Disconnected : ConnectionStatus()
    data class MessageReceived(val response: WebSocketResponse<*>) : ConnectionStatus()
}