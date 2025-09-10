package com.example.network.websocket.model

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonElement

@Serializable
data class WebSocketReceiveMessageDto(
    val type: String,
    val success: Boolean,
    val code: String? = null,
    val data: JsonElement? = null,
)