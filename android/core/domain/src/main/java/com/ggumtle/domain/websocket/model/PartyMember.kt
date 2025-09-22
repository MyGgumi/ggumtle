package com.ggumtle.domain.websocket.model

data class PartyMember(
    val id: Long,
    val nickname: String,
    var isLeader: Boolean = false,
    val isReady: Boolean = false,
    val monggingClassId: Long,
    val monggingLevel: Long,
    val profileImageUrl: String? = null,
)