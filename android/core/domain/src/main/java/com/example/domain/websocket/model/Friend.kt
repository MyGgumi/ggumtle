package com.example.domain.websocket.model

import com.example.domain.model.MemberConnectionState

data class Friend(
    val id: Long,
    val nickname: String,
    val connectionState: MemberConnectionState = MemberConnectionState.OFFLINE,
    val profileImage: String? = null,
    val status: String? = null,
)