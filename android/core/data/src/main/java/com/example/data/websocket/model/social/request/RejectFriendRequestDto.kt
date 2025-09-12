package com.ggumtle.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class RejectFriendRequestDto(
    val friendId: Long
)