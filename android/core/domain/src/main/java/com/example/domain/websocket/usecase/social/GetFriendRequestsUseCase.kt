package com.example.domain.websocket.usecase.social

import com.example.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class GetFriendRequestsUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke() {
        webSocketRepository.getFriendRequests()
    }
}