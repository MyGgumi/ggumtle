package com.example.network.websocket.datasource

import com.example.datastore.AuthManager
import com.example.network.websocket.model.WebSocketState
import com.ggumtle.core.network.BuildConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.merge
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class OkHttpWebSocketDataSource @Inject constructor(
    private val okHttpClient: OkHttpClient,
    private val authManager: AuthManager
) : WebSocketDataSource {

    private var webSocket: WebSocket? = null
    private val _connectionState = MutableSharedFlow<WebSocketState>()
    private val _messages = MutableSharedFlow<String>()

    private val baseUrl = BuildConfig.WS_BASE_URL

    private val webSocketListener = object : WebSocketListener() {
        override fun onOpen(webSocket: WebSocket, response: Response) {
            _connectionState.tryEmit(WebSocketState.Connected)
        }

        override fun onMessage(webSocket: WebSocket, text: String) {
            _messages.tryEmit(text)
        }

        override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
            _connectionState.tryEmit(WebSocketState.Disconnected)
        }

        override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
            _connectionState.tryEmit(WebSocketState.Error(t))
        }
    }

    override fun connect(url: String?): Flow<WebSocketState> = flow {
        emit(WebSocketState.Connecting)
        try {
            val connectionUrl = url ?: baseUrl
            val token = authManager.getAccessToken()

            val request = Request.Builder()
                .url(connectionUrl)
                .apply {
                    if (!token.isNullOrEmpty()) {
                        addHeader("Authorization", "Bearer $token")
                    }
                }
                .build()

            webSocket = okHttpClient.newWebSocket(request, webSocketListener)

            merge(
                _connectionState,
                _messages.map { WebSocketState.MessageReceived(it) }
            ).collect { emit(it) }

        } catch (e: Exception) {
            emit(WebSocketState.Error(e))
        }
    }.flowOn(Dispatchers.IO)

    override fun disconnect() {
        webSocket?.close(1000, "Normal closure")
        webSocket = null
    }

    override fun sendMessage(message: String) {
        webSocket?.send(message)
    }

    override fun observeMessages(): Flow<String> = _messages.asSharedFlow()

    override fun observeConnectionState(): Flow<WebSocketState> =
        _connectionState.asSharedFlow()
}