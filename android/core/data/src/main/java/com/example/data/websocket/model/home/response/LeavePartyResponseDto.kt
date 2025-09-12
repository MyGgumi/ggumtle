package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class LeavePartyResponseDto(
    val leftMemberId: Long,
    val newLeaderId: Long?
)

fun LeavePartyResponseDto.toEvent(): WebSocketEvent.LeavePartySuccess? {
    return WebSocketEvent.LeavePartySuccess(
        leftMemberId = leftMemberId,
        newLeaderId = newLeaderId,
    )
}