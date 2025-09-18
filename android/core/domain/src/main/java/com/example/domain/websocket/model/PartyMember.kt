package com.example.domain.websocket.model

data class PartyMember(
    val id: Long,
    val nickname: String,
    var isLeader: Boolean = false,
    val isReady: Boolean = false,
    val profileImageUrl: String? = null,
)