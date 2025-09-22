package com.ggumtle.data.rest.model.auth.response

import com.ggumtle.domain.rest.model.auth.response.LogoutResponse
import kotlinx.serialization.Serializable

@Serializable
data class LogoutResponseDto(
    val success: Boolean,
)

fun LogoutResponseDto.toDomain() = LogoutResponse(
    success = this.success
)