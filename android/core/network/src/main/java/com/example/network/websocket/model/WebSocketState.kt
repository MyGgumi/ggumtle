package com.example.network.websocket.model

sealed class WebSocketState {
    object Connecting : WebSocketState()
    object Connected : WebSocketState()
    object Disconnected : WebSocketState()
    data class Error(val throwable: Throwable) : WebSocketState()
    data class MessageReceived(val message: String) : WebSocketState()
}