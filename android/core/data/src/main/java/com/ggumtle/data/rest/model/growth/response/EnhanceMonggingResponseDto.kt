package com.ggumtle.data.rest.model.growth.response

import com.ggumtle.domain.rest.model.growth.response.EnhanceMonggingResponse
import kotlinx.serialization.Serializable

@Serializable
data class EnhanceMonggingResponseDto(
    val isSuccess: Boolean,
    val experience: ExperienceDto
)

fun EnhanceMonggingResponseDto.toDomain() = EnhanceMonggingResponse(
    isSuccess = this.isSuccess,
    experience = this.experience.toDomain()
)