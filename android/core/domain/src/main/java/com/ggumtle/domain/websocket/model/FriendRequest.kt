package com.ggumtle.domain.websocket.model

data class FriendRequest(
    val friendRequestId: Long,
    val memberId: Long,
    val nickname: String,
    val fromUserProfileImage: String? = null,
)