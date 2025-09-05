package com.example.common.network.websocket

sealed class WebSocketResult<out T> {
    object Loading : WebSocketResult<Nothing>()
    data class Success<T>(val data: T) : WebSocketResult<T>()
    data class Error(val exception: Throwable) : WebSocketResult<Nothing>()
}