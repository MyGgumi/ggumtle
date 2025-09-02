package com.example.domain.model.auth.request

data class GoogleLoginRequest(
    val idToken: String
) {
    fun isValidToken(): Boolean = idToken.isNotBlank()
}