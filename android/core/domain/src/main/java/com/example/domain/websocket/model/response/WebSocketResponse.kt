package com.example.domain.websocket.model.response

data class WebSocketResponse<T>(
    val type: String,
    val success: Boolean,
    val code: String?,
    val data: T
)