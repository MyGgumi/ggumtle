package com.ggumtle.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class GetProfileFriendDto (
    val memberId: Long
)