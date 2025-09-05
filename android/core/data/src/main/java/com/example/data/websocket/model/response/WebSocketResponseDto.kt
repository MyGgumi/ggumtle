package com.example.data.websocket.model.response

import kotlinx.serialization.Serializable

@Serializable
data class WebSocketResponseDto<T>(
    val type: String,
    val success: Boolean,
    val code: String? = null,
    val data: T
)