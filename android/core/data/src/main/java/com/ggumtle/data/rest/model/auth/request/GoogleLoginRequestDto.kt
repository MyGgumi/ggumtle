package com.ggumtle.data.rest.model.auth.request

import com.ggumtle.domain.rest.model.auth.request.GoogleLoginRequest
import kotlinx.serialization.Serializable

@Serializable
data class GoogleLoginRequestDto(
    val idToken: String
)

fun GoogleLoginRequest.toDto() = GoogleLoginRequestDto(
    idToken = this.idToken
)
