package com.ggumtle.home.model

data class UserProfile(
    val id: Long,
    val nickname: String,
    val profileImageUrl: String? = null
)