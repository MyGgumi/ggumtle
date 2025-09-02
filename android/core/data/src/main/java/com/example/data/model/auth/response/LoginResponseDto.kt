package com.example.data.model.auth.response

import com.example.domain.model.auth.response.LoginResponse
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class LoginResponseDto(
    val accessToken: String,
    val refreshToken: String,
    val email: String,
)

fun LoginResponseDto.toDomain() = LoginResponse(
    accessToken = this.accessToken,
    refreshToken = this.refreshToken,
    email = this.email
)