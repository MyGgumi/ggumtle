package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.PartyMember
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetPartyParticipantsResponseDto(
    val participants: List<PartyMemberDto>
)

@Serializable
data class PartyMemberDto(
    val memberId: Long,
    val nickname: String,
    val isLeader: Boolean,
    val isReady: Boolean,
    val monggingClassId: Long,
    val monggingLevel: Long,
)

fun GetPartyParticipantsResponseDto.toEvent(): WebSocketEvent.GetPartyParticipantsSuccess {
    return WebSocketEvent.GetPartyParticipantsSuccess(
        participants = this.participants.map { partyMember ->
            PartyMember(
                id = partyMember.memberId,
                nickname = partyMember.nickname,
                isLeader = partyMember.isLeader,
                isReady = partyMember.isReady,
                monggingClassId = partyMember.monggingClassId,
                monggingLevel = partyMember.monggingLevel
            )
        }
    )
}