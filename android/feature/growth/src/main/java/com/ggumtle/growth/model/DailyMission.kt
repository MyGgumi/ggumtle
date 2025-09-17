package com.ggumtle.growth.model

data class DailyMission(
    val totalMissions: Int,
    val completedMissions: Int,
    val remainingMissions: Int = totalMissions - completedMissions,
    val missions: List<Mission> = emptyList()
)

data class Mission(
    val id: String,
    val title: String,
    val reward: Int,
    val isCompleted: Boolean = false,
    val isRewarded: Boolean = false
)