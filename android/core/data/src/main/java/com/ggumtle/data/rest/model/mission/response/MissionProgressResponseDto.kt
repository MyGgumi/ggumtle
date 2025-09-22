package com.ggumtle.data.rest.model.mission.response

import com.ggumtle.domain.rest.model.mission.response.MissionProgressResponse
import kotlinx.serialization.Serializable

@Serializable
data class MissionProgressResponseDto(
    val memberMissionId: Long,
    val requiredCount: Int,
    val doneCount: Int,
    val state: String
)

fun MissionProgressResponseDto.toDomain() = MissionProgressResponse(
    memberMissionId = this.memberMissionId,
    requiredCount = this.requiredCount,
    doneCount = this.doneCount,
    state = this.state
)