package com.example.domain.websocket.usecase.home

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class GetPartyParticipantsUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke() {
        webSocketRepository.getPartyParticipants()
    }
}