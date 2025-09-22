package com.ggumtle.data.rest.repository

import com.ggumtle.data.rest.model.mission.response.toDomain
import com.ggumtle.data.rest.remote.datasource.MissionRemoteDataSource
import com.ggumtle.data.rest.util.executeApiCall
import com.ggumtle.domain.rest.model.mission.response.MissionListResponse
import com.ggumtle.domain.rest.model.mission.response.MissionProgressResponse
import com.ggumtle.domain.rest.model.mission.response.MissionRewardResponse
import com.ggumtle.domain.rest.repository.MissionRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class MissionRepositoryImpl @Inject constructor(
    private val remoteDataSource: MissionRemoteDataSource
) : MissionRepository {

    override fun getMissionList(): Flow<Resource<MissionListResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getMissionList() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "미션 목록 조회에 실패했습니다"
        )

    override fun progressMission(memberMissionId: Long): Flow<Resource<MissionProgressResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.progressMission(memberMissionId) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "미션 진행에 실패했습니다"
        )

    override fun claimMissionReward(memberMissionId: Long): Flow<Resource<MissionRewardResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.claimMissionReward(memberMissionId) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "미션 보상 수령에 실패했습니다"
        )
}