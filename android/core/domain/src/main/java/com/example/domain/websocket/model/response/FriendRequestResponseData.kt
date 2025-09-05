package com.example.domain.websocket.model.response

data class FriendRequestResponseData (
    val requesterId: Long,
    val targetMemberId: Long
)