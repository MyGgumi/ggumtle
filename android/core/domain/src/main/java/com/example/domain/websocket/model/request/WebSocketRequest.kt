package com.example.domain.websocket.model.request

data class WebSocketRequest<T>(
    val type: String,
    val data: T
)