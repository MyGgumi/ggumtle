package com.ggumtle.home.model

data class Friend(
    val id: String,
    val nickname: String,
    val profileImageUrl: String? = null,
    val isOnline: Boolean = false
)