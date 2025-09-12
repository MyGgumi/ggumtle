package com.ggumtle.data.websocket.model.home.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
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