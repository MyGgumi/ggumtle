package com.example.data.websocket.model.response

import com.example.domain.websocket.model.response.FriendRequestResponseData
import kotlinx.serialization.Serializable

@Serializable
data class FriendRequestResponseDto(
    val requesterId: Long,
    val targetMemberId: Long
)

fun FriendRequestResponseDto.toDomain(): FriendRequestResponseData {
    return FriendRequestResponseData(
        requesterId = this.requesterId,
        targetMemberId = this.targetMemberId
    )
}

fun FriendRequestResponseData.toDto(): FriendRequestResponseDto {
    return FriendRequestResponseDto(
        requesterId = this.requesterId,
        targetMemberId = this.targetMemberId
    )
}