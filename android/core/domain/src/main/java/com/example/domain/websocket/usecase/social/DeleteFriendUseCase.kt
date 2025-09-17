package com.example.domain.websocket.usecase.social

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class DeleteFriendUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke(friendId : Long) {
        webSocketRepository.deleteFriendRequest(friendId)
    }
}