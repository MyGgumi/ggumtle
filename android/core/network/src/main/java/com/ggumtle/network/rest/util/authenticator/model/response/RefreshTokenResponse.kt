package com.ggumtle.network.rest.util.authenticator.model.response

import kotlinx.serialization.Serializable

@Serializable
data class RefreshTokenResponse(
    val accessToken: String,
    val refreshToken: String,
    val email: String
)