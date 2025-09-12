package com.ggumtle.data.rest.model.auth.response

import com.ggumtle.domain.rest.model.auth.response.LoginResponse
import kotlinx.serialization.Serializable

@Serializable
data class LoginResponseDto(
    val memberId: Long,
    val accessToken: String,
//    val refreshToken: String,
//    val email: String,
)

fun LoginResponseDto.toDomain() = LoginResponse(
    accessToken = this.accessToken,
    memberId = this.memberId
//    refreshToken = this.refreshToken,
//    email = this.email
)