package com.ggumtle.home.model

data class PartyMember(
    val id: Long,
    val nickname: String,
    val profileImageUrl: String? = null,
    val isReady: Boolean = false,
    var isLeader: Boolean = false
)