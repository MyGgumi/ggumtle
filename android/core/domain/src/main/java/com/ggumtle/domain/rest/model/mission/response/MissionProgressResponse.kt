package com.ggumtle.domain.rest.model.mission.response

data class MissionProgressResponse(
    val memberMissionId: Long,
    val requiredCount: Int,
    val doneCount: Int,
    val state: String
)