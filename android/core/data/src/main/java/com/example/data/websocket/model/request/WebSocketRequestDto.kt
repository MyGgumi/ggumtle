package com.example.data.websocket.model.request

import kotlinx.serialization.Serializable

@Serializable
data class WebSocketRequestDto<T>(
    val type: String,
    val data: T
)