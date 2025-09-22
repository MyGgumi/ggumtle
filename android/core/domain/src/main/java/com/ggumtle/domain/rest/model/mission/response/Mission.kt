package com.ggumtle.domain.rest.model.mission.response

data class Mission(
    val memberMissionId: Long,
    val name: String,
    val description: String,
    val requiredCount: Int,
    val doneCount: Int,
    val state: String
)