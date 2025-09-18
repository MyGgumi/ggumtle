package com.example.data.rest.model.user.response

import com.example.domain.rest.model.user.response.UserInfoResponse
import kotlinx.serialization.Serializable

@Serializable
data class UserInfoResponseDto(
    val nickname: String,
    val coin: Int,
)

fun UserInfoResponseDto.toDomain() = UserInfoResponse(
    nickname = this.nickname,
    coin = this.coin
)