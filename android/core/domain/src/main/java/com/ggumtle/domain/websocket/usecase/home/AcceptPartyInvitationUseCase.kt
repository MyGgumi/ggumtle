package com.ggumtle.domain.websocket.usecase.home

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class AcceptPartyInvitationUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke(invitationId: String) {
        webSocketRepository.acceptPartyInvitation(invitationId)
    }
}