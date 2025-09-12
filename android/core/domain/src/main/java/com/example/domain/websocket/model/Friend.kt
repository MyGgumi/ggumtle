package com.ggumtle.domain.websocket.model

import com.ggumtle.domain.model.MemberConnectionState

data class Friend(
    val id: Long,
    val nickname: String,
    val connectionState: MemberConnectionState = MemberConnectionState.OFFLINE,
    val profileImage: String? = null,
    val status: String? = null,
)