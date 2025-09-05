package com.example.domain.websocket.usecase.social.send

import com.example.common.network.websocket.WebSocketResult
import com.example.domain.websocket.model.MessageTypes
import com.example.domain.websocket.model.request.FriendRequestData
import com.example.domain.websocket.repository.WebSocketRepository
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class SendFriendRequestUseCase @Inject constructor(
    private val repository: WebSocketRepository
) {
    operator fun invoke(targetMemberId: Long): Flow<WebSocketResult<Unit>> =
        repository.sendMessage(
            type = MessageTypes.REQUEST_FRIEND,
            data = FriendRequestData(targetMemberId)
        )
}