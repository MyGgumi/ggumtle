package com.ggumtle.domain.rest.model.growth.response

data class MonggingDetailResponse(
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