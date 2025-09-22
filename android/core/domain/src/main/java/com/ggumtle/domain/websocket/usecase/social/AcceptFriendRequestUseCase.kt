package com.ggumtle.domain.websocket.usecase.social

import com.ggumtle.domain.websocket.repository.WebSocketRepository
import javax.inject.Inject

class AcceptFriendRequestUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    suspend operator fun invoke(friendId : Long) {
        webSocketRepository.acceptFriend(friendId)
    }
}