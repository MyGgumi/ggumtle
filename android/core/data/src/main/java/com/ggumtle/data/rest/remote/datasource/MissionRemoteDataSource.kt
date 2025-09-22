package com.ggumtle.data.rest.remote.datasource

import com.ggumtle.data.rest.model.mission.response.MissionListResponseDto
import com.ggumtle.data.rest.model.mission.response.MissionProgressResponseDto
import com.ggumtle.data.rest.model.mission.response.MissionRewardResponseDto
import com.ggumtle.data.rest.remote.service.MissionService
import com.ggumtle.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class MissionRemoteDataSource @Inject constructor(
    private val missionService: MissionService
) {
    suspend fun getMissionList(): NetworkResult<MissionListResponseDto> =
        missionService.getMissionList()

    suspend fun progressMission(memberMissionId: Long): NetworkResult<MissionProgressResponseDto> =
        missionService.progressMission(memberMissionId)

    suspend fun claimMissionReward(memberMissionId: Long): NetworkResult<MissionRewardResponseDto> =
        missionService.claimMissionReward(memberMissionId)
}