package com.example.domain.model.auth.response

data class LoginResponse(
    val accessToken: String,
    val refreshToken: String,
    val email: String
) {
    fun isTokenValid(): Boolean = accessToken.isNotBlank() && refreshToken.isNotBlank()
}