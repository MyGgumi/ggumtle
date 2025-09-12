package com.example.domain.websocket.usecase.home

import com.example.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class GetInvitationsUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke() {
        webSocketRepository.getInvitations()
    }
}