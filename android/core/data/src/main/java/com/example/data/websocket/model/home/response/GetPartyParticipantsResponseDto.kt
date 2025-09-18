package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.PartyMember
import com.ggumtle.data.websocket.model.home.response.GetInvitationsResponseDto
import com.ggumtle.domain.websocket.model.Invitation
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetPartyParticipantsResponseDto(
    val participants: List<PartyMemberDto>
)

@Serializable
data class PartyMemberDto(
    val memberid: Long,
    val nickname: String,
    val isLeader: Boolean,
    val isReady: Boolean
)

fun GetPartyParticipantsResponseDto.toEvent(): WebSocketEvent.GetPartyParticipantsSuccess {
    return WebSocketEvent.GetPartyParticipantsSuccess(
        participants = this.participants.map { partyMember ->
            PartyMember(
                id = partyMember.memberid,
                nickname = partyMember.nickname,
                isLeader = partyMember.isLeader,
                isReady = partyMember.isReady,
            )
        }
    )
}