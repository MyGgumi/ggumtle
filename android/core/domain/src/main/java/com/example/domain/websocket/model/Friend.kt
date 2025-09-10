package com.example.domain.websocket.model

data class Friend(
    val id: Long,
    val nickname: String,
    val profileImage: String? = null,
    val isOnline: Boolean = false,
    val status: String? = null
)