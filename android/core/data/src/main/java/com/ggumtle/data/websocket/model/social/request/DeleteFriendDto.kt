package com.ggumtle.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class DeleteFriendDto (
    val friendId: Long
)