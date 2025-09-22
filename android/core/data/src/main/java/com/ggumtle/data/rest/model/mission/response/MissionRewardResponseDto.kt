package com.ggumtle.data.rest.model.mission.response

import com.ggumtle.domain.rest.model.mission.response.MissionRewardResponse
import kotlinx.serialization.Serializable

@Serializable
data class MissionRewardResponseDto(
    val reward: Int,
    val currentCoin: Int
)

fun MissionRewardResponseDto.toDomain() = MissionRewardResponse(
    reward = this.reward,
    currentCoin = this.currentCoin
)