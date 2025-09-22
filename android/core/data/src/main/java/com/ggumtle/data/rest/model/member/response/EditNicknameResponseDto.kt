package com.ggumtle.data.rest.model.member.response

import com.ggumtle.domain.rest.model.member.response.EditNicknameResponse
import kotlinx.serialization.Serializable

@Serializable
data class EditNicknameResponseDto(
    val success: Boolean
)

fun EditNicknameResponseDto.toDomain() = EditNicknameResponse(
    success = this.success
)