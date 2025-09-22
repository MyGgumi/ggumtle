package com.ggumtle.data.rest.model.member.response

import com.ggumtle.domain.rest.model.member.response.MemberCoinResponse
import kotlinx.serialization.Serializable

@Serializable
data class MemberCoinResponseDto(
    val memberId: Long,
    val coin: Int,
)

fun MemberCoinResponseDto.toDomain() = MemberCoinResponse(
    memberId = this.memberId,
    coin = this.coin
)