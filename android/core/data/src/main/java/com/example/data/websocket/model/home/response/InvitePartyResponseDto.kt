package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class InvitePartyResponseDto(
    val invitationId: String,
    val inviterNickname: String
)

fun InvitePartyResponseDto.toEvent(): WebSocketEvent.InvitePartySuccess? {
    return WebSocketEvent.InvitePartySuccess(
        invitationId = invitationId,
        inviterNickname = inviterNickname
    )
}