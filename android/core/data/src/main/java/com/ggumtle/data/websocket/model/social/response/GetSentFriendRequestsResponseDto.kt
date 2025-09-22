package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.websocket.model.SentRequest
import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetSentFriendRequestsResponseDto(
    val friendRequests: List<SentFriendRequestsResponseDto>
)

@Serializable
data class SentFriendRequestsResponseDto(
    val friendId: Long,
    val memberId: Long,
    val nickname: String
)

fun GetSentFriendRequestsResponseDto.toEvent(): WebSocketEvent.GetSentFriendRequestsSuccess {
    return WebSocketEvent.GetSentFriendRequestsSuccess(
        sentFriendRequests = this.friendRequests.map { friendResponse ->
            SentRequest(
                id = friendResponse.friendId,
                toUserId = friendResponse.memberId,
                toUserName = friendResponse.nickname
            )
        }
    )
}