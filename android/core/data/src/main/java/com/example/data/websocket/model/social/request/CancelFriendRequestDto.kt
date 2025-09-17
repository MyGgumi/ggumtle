package com.example.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class CancelFriendRequestDto (
    val friendId: Long
)