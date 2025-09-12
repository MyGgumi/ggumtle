package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.Friend
import com.example.domain.websocket.model.Invitation
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetInvitationsResponseDto(
    val invitations: List<InvitationDto>
)

@Serializable
data class InvitationDto(
    val invitationId: String,
    val inviterNickname: String
)

fun GetInvitationsResponseDto.toEvent(): WebSocketEvent.GetInvitationsSuccess {
    return WebSocketEvent.GetInvitationsSuccess(
        invitations = this.invitations.map { invitations ->
            Invitation(
                invitationId = invitations.invitationId,
                inviterNickname = invitations.inviterNickname
            )
        }
    )
}