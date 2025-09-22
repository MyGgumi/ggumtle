package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class CancelFriendRequestResponseDto(
    val friendId: Long,
    val followerId: Long,
    val followerNickname: String,
    val followeeId: Long,
    val followeeNickname: String,
)

fun CancelFriendRequestResponseDto.toEvent(): WebSocketEvent.CancelFriendRequestSuccess? {
    return WebSocketEvent.CancelFriendRequestSuccess(
        friendRequestId = friendId,
        followerId = followerId,
        followerNickname = followerNickname,
        followeeId = followeeId,
        followeeNickname = followeeNickname,
    )
}