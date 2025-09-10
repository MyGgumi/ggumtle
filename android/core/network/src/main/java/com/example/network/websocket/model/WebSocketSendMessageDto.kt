package com.example.network.websocket.model

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonElement

@Serializable
data class WebSocketSendMessageDto(
    val type: String,
    val data: JsonElement? = null,
)