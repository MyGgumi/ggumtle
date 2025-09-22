package com.ggumtle.data.rest.remote.service

import com.ggumtle.data.rest.model.mission.response.MissionListResponseDto
import com.ggumtle.data.rest.model.mission.response.MissionProgressResponseDto
import com.ggumtle.data.rest.model.mission.response.MissionRewardResponseDto
import com.ggumtle.network.rest.model.NetworkResult
import retrofit2.http.*

interface MissionService {

    @GET("mission")
    suspend fun getMissionList(): NetworkResult<MissionListResponseDto>

    @PATCH("mission/{member-mission-id}")
    suspend fun progressMission(
        @Path("member-mission-id") memberMissionId: Long
    ): NetworkResult<MissionProgressResponseDto>

    @PATCH("mission/{member-mission-id}/reward")
    suspend fun claimMissionReward(
        @Path("member-mission-id") memberMissionId: Long
    ): NetworkResult<MissionRewardResponseDto>

}