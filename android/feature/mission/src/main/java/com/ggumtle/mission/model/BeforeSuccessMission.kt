package com.ggumtle.mission.model

import com.ggumtle.domain.rest.model.mission.response.Mission
import com.ggumtle.domain.rest.model.mission.response.MissionListResponse

data class BeforeSuccessMission(
    val missionId: Long,
    val memberMissionId: Long,
    val missionName: String,
    val remainCount: Int,
)

fun Mission.toBeforeSuccessMission(): BeforeSuccessMission {
    return BeforeSuccessMission(
        missionId = this.missionId,
        memberMissionId = this.memberMissionId,
        missionName = this.name,
        remainCount = this.requiredCount - this.doneCount
    )
}