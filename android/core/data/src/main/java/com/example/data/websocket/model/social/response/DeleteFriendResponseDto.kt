package com.example.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class DeleteFriendResponseDto(
    val followerId: Long,
    val followerNickname: String,
    val followeeId: Long,
    val followeeNickname: String,
)

fun DeleteFriendResponseDto.toEvent(): WebSocketEvent.DeleteFriendRequestSuccess? {
    return WebSocketEvent.DeleteFriendRequestSuccess(
        followerId = followerId,
        followerNickname = followerNickname,
        followeeId = followeeId,
        followeeNickname = followeeNickname,
    )
}