package com.ggumtle.data.rest.model.mission.response

import com.ggumtle.domain.rest.model.mission.response.MissionListResponse
import kotlinx.serialization.Serializable

@Serializable
data class MissionListResponseDto(
    val missions: List<MissionDto>
)

fun MissionListResponseDto.toDomain() = MissionListResponse(
    missions = this.missions.map { it.toDomain() }
)