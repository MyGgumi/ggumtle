package com.ggumtle.data.rest.model.member.response

import com.ggumtle.domain.rest.model.member.response.DeleteAccountResponse
import kotlinx.serialization.Serializable

@Serializable
data class DeleteAccountResponseDto(
    val success: Boolean,
)

fun DeleteAccountResponseDto.toDomain(): DeleteAccountResponse {
    return DeleteAccountResponse(
        success = success
    )
}