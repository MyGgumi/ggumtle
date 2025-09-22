package com.ggumtle.data.rest.model.member.request

import com.ggumtle.domain.rest.model.member.request.EditNicknameRequest
import kotlinx.serialization.Serializable

@Serializable
data class EditNicknameRequestDto(
    val nickname: String
)

fun EditNicknameRequest.toDto() = EditNicknameRequestDto(
    nickname = this.nickname
)
