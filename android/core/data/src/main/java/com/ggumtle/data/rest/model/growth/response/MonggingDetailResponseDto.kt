package com.ggumtle.data.rest.model.growth.response

import com.ggumtle.domain.rest.model.growth.response.MonggingDetailResponse
import kotlinx.serialization.Serializable

@Serializable
data class MonggingDetailResponseDto(
    val id: Long,
    val monggingClass: String,
    val nowLevel: Int,
    val nowPercentage: Double,
    val isMaxLevel: Boolean,
    val afterLevel: Int? = null,
    val afterPercentage: Double? = null,
    val needCoin: Int? = null,
    val successPercentage: Int? = null
)

fun MonggingDetailResponseDto.toDomain() = MonggingDetailResponse(
    id = this.id,
    monggingClass = this.monggingClass,
    nowLevel = this.nowLevel,
    nowPercentage = this.nowPercentage,
    isMaxLevel = this.isMaxLevel,
    afterLevel = this.afterLevel,
    afterPercentage = this.afterPercentage,
    needCoin = this.needCoin,
    successPercentage = this.successPercentage
)