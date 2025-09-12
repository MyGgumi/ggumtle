package com.example.data.websocket.model.home.response

import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class CreatePartyResponseDto(
    val partyId: String
)

fun CreatePartyResponseDto.toEvent(): WebSocketEvent.CreatePartySuccess? {
    return WebSocketEvent.CreatePartySuccess(
        partyId = partyId
    )
}