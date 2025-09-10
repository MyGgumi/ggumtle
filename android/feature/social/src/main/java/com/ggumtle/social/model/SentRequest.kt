package com.ggumtle.social.model

data class SentRequest(
    val id: Long,
    val toUserId: Long,
    val toUserName: String,
    val toUserProfileImage: String? = null,
    val timestamp: Long
)