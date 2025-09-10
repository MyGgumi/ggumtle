package com.example.network.websocket.client

import android.util.Log
import com.example.datastore.AuthManager
import com.example.network.websocket.model.WebSocketConnectionState
import com.example.network.websocket.model.WebSocketReceiveMessageDto
import com.example.network.websocket.model.WebSocketSendMessageDto
import com.ggumtle.core.network.BuildConfig
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.serialization.json.Json
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import javax.inject.Inject
import javax.inject.Named

class WebSocketClientImpl @Inject constructor(
    @Named("websocket") private val okHttpClient: OkHttpClient,
    private val authManager: AuthManager
) : WebSocketClient {

    private var webSocket: WebSocket? = null
    private val json = Json {
        ignoreUnknownKeys = true
        encodeDefaults = false
    }

    private val _connectionState = MutableStateFlow(WebSocketConnectionState.DISCONNECTED)
    override val connectionState: Flow<WebSocketConnectionState> = _connectionState.asStateFlow()

    private val _messages = MutableSharedFlow<WebSocketReceiveMessageDto>(
        replay = 0,
        extraBufferCapacity = 64,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )
    override val messages: Flow<WebSocketReceiveMessageDto> = _messages.asSharedFlow()

    private val webSocketListener = object : WebSocketListener() {
        override fun onOpen(webSocket: WebSocket, response: Response) {
            Log.d("WebSocketClient", "WebSocket connection opened")
            _connectionState.value = WebSocketConnectionState.CONNECTED
        }

        override fun onMessage(webSocket: WebSocket, text: String) {
            try {
                val responseDto = json.decodeFromString<WebSocketReceiveMessageDto>(text)
                _messages.tryEmit(responseDto)
                Log.d("WebSocketClient", "Message received: $responseDto")
            } catch (e: Exception) {
                Log.e("WebSocketClient", "Failed to parse message: $text", e)
            }
        }

        override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
            Log.d("WebSocketClient", "WebSocket closing: $code $reason")
            _connectionState.value = WebSocketConnectionState.DISCONNECTED
        }

        override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
            Log.d("WebSocketClient", "WebSocket closed: $code $reason")
            _connectionState.value = WebSocketConnectionState.DISCONNECTED
        }

        override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
            Log.e("WebSocketClient", "WebSocket connection failed", t)
            _connectionState.value = WebSocketConnectionState.FAILED
        }
    }

    override suspend fun connect() {
        if (_connectionState.value == WebSocketConnectionState.CONNECTED ||
            _connectionState.value == WebSocketConnectionState.CONNECTING) {
            return
        }

        _connectionState.value = WebSocketConnectionState.CONNECTING

        try {
            val token = authManager.getAccessToken()
            val baseUrl = BuildConfig.WS_BASE_URL

            val request = Request.Builder()
                .url(baseUrl)
                .apply {
                    if (!token.isNullOrEmpty()) {
                        addHeader("Authorization", "Bearer $token")
                    }
                }
                .build()

            webSocket = okHttpClient.newWebSocket(request, webSocketListener)
        } catch (e: Exception) {
            Log.e("WebSocketClient", "Failed to connect", e)
            _connectionState.value = WebSocketConnectionState.FAILED
        }
    }

    override suspend fun disconnect() {
        webSocket?.close(1000, "Client disconnect")
        webSocket = null
        _connectionState.value = WebSocketConnectionState.DISCONNECTED
    }

    override suspend fun sendMessage(message: WebSocketSendMessageDto) {
        val currentWebSocket = webSocket
        if (currentWebSocket == null || _connectionState.value != WebSocketConnectionState.CONNECTED) {
            throw IllegalStateException("WebSocket is not connected")
        }

        try {
            val jsonMessage = json.encodeToString(WebSocketSendMessageDto.serializer(), message)
            val success = currentWebSocket.send(jsonMessage)
            if (!success) {
                throw IllegalStateException("Failed to send message")
            }
            Log.d("WebSocketClient", "Message sent: $message")
        } catch (e: Exception) {
            Log.e("WebSocketClient", "Failed to send message", e)
            throw e
        }
    }
}