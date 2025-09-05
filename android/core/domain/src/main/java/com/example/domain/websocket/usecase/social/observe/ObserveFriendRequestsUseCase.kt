package com.example.domain.websocket.usecase.social.observe

import com.example.common.network.websocket.WebSocketResult
import com.example.domain.websocket.model.MessageTypes
import com.example.domain.websocket.model.response.FriendRequestResponseData
import com.example.domain.websocket.model.result.FriendRequestResult
import com.example.domain.websocket.repository.WebSocketRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ObserveFriendRequestsUseCase @Inject constructor(
    private val webSocketRepository: WebSocketRepository
) {
    operator fun invoke(): Flow<WebSocketResult<FriendRequestResult>> =
        webSocketRepository.observeMessages()
            .filter { result ->
                result is WebSocketResult.Success &&
                        result.data.type == MessageTypes.REQUEST_FRIEND
            }
            .map { result ->
                when (result) {
                    is WebSocketResult.Success -> {
                        val response = result.data
                        if (response.success) {
                            val data = response.data as FriendRequestResponseData
                            WebSocketResult.Success(
                                FriendRequestResult.Success(
                                    requesterId = data.requesterId,
                                    targetMemberId = data.targetMemberId
                                )
                            )
                        } else {
                            val errorMessage = response.data as String
                            WebSocketResult.Success(
                                FriendRequestResult.Error(errorMessage)
                            )
                        }
                    }
                    is WebSocketResult.Error -> result as WebSocketResult<FriendRequestResult>
                    is WebSocketResult.Loading -> result as WebSocketResult<FriendRequestResult>
                }
            }
}