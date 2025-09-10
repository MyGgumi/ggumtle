package com.example.data.websocket.model.social.response

import com.example.domain.model.Member
import com.example.domain.model.MemberStatus
import com.example.domain.websocket.model.WebSocketEvent
import kotlinx.serialization.Serializable

@Serializable
data class SearchMemberDataResponseDto(
    val hasNext: Boolean,
    val members: List<MemberDto>
)

@Serializable
data class MemberDto(
    val memberId: Long,
    val nickname: String,
    val status: String
)

fun SearchMemberDataResponseDto.toEvent(): WebSocketEvent.MemberSearchSuccess? {
    return WebSocketEvent.MemberSearchSuccess(
        hasNext = hasNext,
        members = members.map { it.toDomain() }
    )
}

fun MemberDto.toDomain(): Member {
    return Member(
        memberId = this.memberId,
        nickname = this.nickname,
        status = MemberStatus.valueOf(this.status)
    )
}