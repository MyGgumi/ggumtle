package com.ggumtle.data.websocket.model.social.response

import com.ggumtle.domain.model.MemberConnectionState
import com.ggumtle.domain.websocket.model.Friend
import com.ggumtle.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class GetFriendsResponseDto(
    val friends: List<FriendsResponseDto>
)

@Serializable
data class FriendsResponseDto(
    val memberId: Long,
    val nickname: String,
    val memberState: String
)

fun GetFriendsResponseDto.toEvent(): WebSocketEvent.GetFriendsSuccess {
    return WebSocketEvent.GetFriendsSuccess(
        friends = this.friends.map { friendResponse ->
            Friend(
                id = friendResponse.memberId,
                nickname = friendResponse.nickname,
                connectionState = MemberConnectionState.fromString(friendResponse.memberState)
            )
        }
    )
}