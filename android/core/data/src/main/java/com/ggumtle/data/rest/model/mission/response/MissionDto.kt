package com.ggumtle.data.rest.model.mission.response

import com.ggumtle.domain.rest.model.mission.response.Mission
import kotlinx.serialization.Serializable

@Serializable
data class MissionDto(
    val memberMissionId: Long,
    val missionId: Long,
    val name: String,
    val description: String,
    val requiredCount: Int,
    val doneCount: Int,
    val state: String
)

fun MissionDto.toDomain() = Mission(
    memberMissionId = this.memberMissionId,
    missionId = this.missionId,
    name = this.name,
    description = this.description,
    requiredCount = this.requiredCount,
    doneCount = this.doneCount,
    state = this.state
)