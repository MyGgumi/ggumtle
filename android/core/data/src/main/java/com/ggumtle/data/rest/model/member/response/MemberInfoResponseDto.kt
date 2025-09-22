package com.ggumtle.data.rest.model.member.response

import com.ggumtle.domain.rest.model.member.response.MemberInfoResponse
import kotlinx.serialization.Serializable

@Serializable
data class MemberInfoResponseDto(
    val nickname: String,
    val coin: Int,
)

fun MemberInfoResponseDto.toDomain() = MemberInfoResponse(
    nickname = this.nickname,
    coin = this.coin
)