package com.example.domain.rest.model.auth.request

data class GoogleLoginRequest(
    val idToken: String
) {
    fun isValidToken(): Boolean = idToken.isNotBlank()
}