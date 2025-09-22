package com.ggumtle.data.rest.model.growth.response

import com.ggumtle.domain.rest.model.growth.response.Mongging
import kotlinx.serialization.Serializable

@Serializable
data class MonggingDto(
    val id: Long,
    val monggingClass: String,
    val level: Int
)

fun MonggingDto.toDomain() = Mongging(
    id = this.id,
    monggingClass = this.monggingClass,
    level = this.level
)