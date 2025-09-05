package com.example.data.websocket.model.request

import com.example.domain.websocket.model.request.FriendRequestData
import kotlinx.serialization.Serializable

@Serializable
data class FriendRequestDto(
    val targetMemberId: Long
)

fun FriendRequestDto.toDomain(): FriendRequestData {
    return FriendRequestData(
        targetMemberId = this.targetMemberId
    )
}

fun FriendRequestData.toDto(): FriendRequestDto {
    return FriendRequestDto(
        targetMemberId = this.targetMemberId
    )
}