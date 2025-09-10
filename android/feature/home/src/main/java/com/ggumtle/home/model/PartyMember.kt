package com.ggumtle.home.model

data class PartyMember(
    val id: String,
    val nickname: String,
    val profileImageUrl: String? = null,
    val isReady: Boolean = false,
    val isLeader: Boolean = false
)