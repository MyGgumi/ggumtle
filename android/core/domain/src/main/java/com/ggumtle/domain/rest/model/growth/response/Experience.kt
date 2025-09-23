package com.ggumtle.domain.rest.model.growth.response

data class Experience(
    val monggingId: Long,
    val statisticName: String,
    val beforePercentage: Double,
    val afterPercentage: Double,
    val beforeLevel: Int,
    val afterLevel: Int,
    val nextSuccessRate: Int
)