package com.ggumtle.data.rest.model.growth.response

import com.ggumtle.domain.rest.model.growth.response.MonggingListResponse
import kotlinx.serialization.Serializable

@Serializable
data class MonggingListResponseDto(
    val monggings: List<MonggingDto>
)

fun MonggingListResponseDto.toDomain() = MonggingListResponse(
    monggings = this.monggings.map { it.toDomain() }
)