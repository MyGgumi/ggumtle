package com.example.data.websocket.model.social.response

import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class AcceptFriendResponseDto(
    val followerId: Long,
    val followerNickname: String,
    val followeeId: Long,
    val followeeNickname: String
)

fun AcceptFriendResponseDto.toEvent(): WebSocketEvent.AcceptFriendRequestSuccess? {
    return WebSocketEvent.AcceptFriendRequestSuccess(
        followerId = followerId,
        followerNickname = followerNickname,
        followeeId = followeeId,
        followeeNickname = followeeNickname,
    )
}