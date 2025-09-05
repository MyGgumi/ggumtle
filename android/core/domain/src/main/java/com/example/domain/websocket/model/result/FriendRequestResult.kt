package com.example.domain.websocket.model.result

sealed class FriendRequestResult {
    data class Success(
        val requesterId: Long,
        val targetMemberId: Long
    ) : FriendRequestResult()

    data class Error(val message: String) : FriendRequestResult()
}