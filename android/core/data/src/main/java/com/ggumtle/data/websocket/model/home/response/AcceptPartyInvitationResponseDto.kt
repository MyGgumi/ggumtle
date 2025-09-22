package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class AcceptPartyInvitationResponseDto(
    val joinedMemberId: Long,
    val joinedMemberNickname: String,
    val monggingClassId: Long,
    val monggingLevel: Long,
)

fun AcceptPartyInvitationResponseDto.toEvent(): WebSocketEvent.AcceptPartyInvitationSuccess? {
    return WebSocketEvent.AcceptPartyInvitationSuccess(
        joinedMemberId = joinedMemberId,
        joinedMemberNickname = joinedMemberNickname,
        monggingClassId = monggingClassId,
        monggingLevel = monggingLevel
    )
}