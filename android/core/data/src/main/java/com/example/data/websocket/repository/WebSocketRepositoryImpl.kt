package com.example.data.websocket.repository

import com.example.common.network.websocket.WebSocketResult
import com.example.data.websocket.mapper.MessageMapper
import com.example.domain.websocket.model.ConnectionStatus
import com.example.domain.websocket.model.response.WebSocketResponse
import com.example.domain.websocket.repository.WebSocketRepository
import com.example.network.websocket.datasource.WebSocketDataSource
import com.example.network.websocket.model.WebSocketState
import kotlinx.coroutines.flow.*
import javax.inject.*

@Singleton
class WebSocketRepositoryImpl @Inject constructor(
    private val webSocketDataSource: WebSocketDataSource,
    private val messageMapper: MessageMapper
) : WebSocketRepository {

    override fun connectToWebSocket(url: String?): Flow<WebSocketResult<ConnectionStatus>> =
        webSocketDataSource.connect(url)
            .map { state ->
                when (state) {
                    WebSocketState.Connected -> WebSocketResult.Success(ConnectionStatus.Connected)
                    WebSocketState.Connecting -> WebSocketResult.Loading
                    WebSocketState.Disconnected -> WebSocketResult.Success(ConnectionStatus.Disconnected)
                    is WebSocketState.Error -> WebSocketResult.Error(state.throwable)
                    is WebSocketState.MessageReceived -> {
                        messageMapper.parseResponseJson(state.message)
                            .fold(
                                onSuccess = { response ->
                                    WebSocketResult.Success(ConnectionStatus.MessageReceived(response))
                                },
                                onFailure = { exception ->
                                    WebSocketResult.Error(exception)
                                }
                            )
                    }
                }
            }
            .catch { emit(WebSocketResult.Error(it)) }

    override fun <T> sendMessage(type: String, data: T): Flow<WebSocketResult<Unit>> = flow {
        emit(WebSocketResult.Loading)

        messageMapper.createRequestJson(type, data)
            .fold(
                onSuccess = { jsonMessage ->
                    try {
                        webSocketDataSource.sendMessage(jsonMessage)
                        emit(WebSocketResult.Success(Unit))
                    } catch (e: Exception) {
                        emit(WebSocketResult.Error(e))
                    }
                },
                onFailure = { exception ->
                    emit(WebSocketResult.Error(exception))
                }
            )
    }

    override fun observeMessages(): Flow<WebSocketResult<WebSocketResponse<*>>> =
        webSocketDataSource.observeMessages()
            .map { jsonMessage ->
                messageMapper.parseResponseJson(jsonMessage)
                    .fold(
                        onSuccess = { response -> WebSocketResult.Success(response) },
                        onFailure = { exception -> WebSocketResult.Error(exception) }
                    )
            }
            .catch { emit(WebSocketResult.Error(it)) }

    override fun disconnect() {
        webSocketDataSource.disconnect()
    }
}