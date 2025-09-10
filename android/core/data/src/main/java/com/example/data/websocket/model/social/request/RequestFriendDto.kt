package com.example.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class RequestFriendDto(
    val targetMemberId: Long
)