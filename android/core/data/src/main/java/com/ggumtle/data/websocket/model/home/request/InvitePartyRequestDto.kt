package com.ggumtle.data.websocket.model.home.request

import kotlinx.serialization.Serializable

@Serializable
data class InvitePartyRequestDto(
    val inviteeId: Long
)