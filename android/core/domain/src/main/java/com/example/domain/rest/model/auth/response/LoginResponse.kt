package com.example.domain.rest.model.auth.response

data class LoginResponse(
    val memberId: Long,
    val accessToken: String,
//    val refreshToken: String,
//    val email: String
) {
    fun isTokenValid(): Boolean = accessToken.isNotBlank() //&& refreshToken.isNotBlank()
}