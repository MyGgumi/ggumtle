package com.ggumtle.growth.model

import com.ggumtle.domain.rest.model.mission.response.Mission
import com.ggumtle.domain.rest.model.mission.response.MissionListResponse

data class DailyMission(
    val totalMissionsCount: Int,
    val completedMissionsCount: Int,
    val remainingMissionsCount: Int,
    val afterRewordMissionsCount: Int,
    val missions: List<Mission> = emptyList()
)

fun MissionListResponse.toDailyMission(): DailyMission {
    return DailyMission(
        totalMissionsCount = this.missions.size,
        completedMissionsCount = this.missions.filter { it.state == "SUCCESS" }.size,
        remainingMissionsCount = this.missions.filter { it.state == "BEFORE_SUCCESS" }.size,
        afterRewordMissionsCount = this.missions.filter { it.state == "AFTER_REWARD" }.size,
        missions = this.missions
    )
}