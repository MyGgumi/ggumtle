package com.ggumtle.data.rest.model.growth.response

import com.ggumtle.domain.rest.model.growth.response.Experience
import kotlinx.serialization.Serializable

@Serializable
data class ExperienceDto(
    val monggingId: Long,
    val statisticName: String,
    val beforePercentage: Double,
    val afterPercentage: Double,
    val beforeLevel: Int,
    val afterLevel: Int,
    val nextSuccessRate: Int
)

fun ExperienceDto.toDomain() = Experience(
    monggingId = this.monggingId,
    statisticName = this.statisticName,
    beforePercentage = this.beforePercentage,
    afterPercentage = this.afterPercentage,
    beforeLevel = this.beforeLevel,
    afterLevel = this.afterLevel,
    nextSuccessRate = this.nextSuccessRate
)