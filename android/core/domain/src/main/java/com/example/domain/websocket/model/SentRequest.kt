package com.example.domain.websocket.model

data class SentRequest(
    val id: Long,
    val toUserId: Long,
    val toUserName: String,
    val toUserProfileImage: String? = null,
)