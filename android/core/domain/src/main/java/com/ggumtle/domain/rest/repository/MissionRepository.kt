package com.ggumtle.domain.rest.repository

import com.ggumtle.domain.rest.model.mission.response.MissionListResponse
import com.ggumtle.domain.rest.model.mission.response.MissionProgressResponse
import com.ggumtle.domain.rest.model.mission.response.MissionRewardResponse
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow

interface MissionRepository {
    fun getMissionList(): Flow<Resource<MissionListResponse>>
    fun progressMission(memberMissionId: Long): Flow<Resource<MissionProgressResponse>>
    fun claimMissionReward(memberMissionId: Long): Flow<Resource<MissionRewardResponse>>
}