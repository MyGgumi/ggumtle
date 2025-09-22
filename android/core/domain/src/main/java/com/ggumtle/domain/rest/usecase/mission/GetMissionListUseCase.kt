package com.ggumtle.domain.rest.usecase.mission

import com.ggumtle.domain.rest.model.mission.response.MissionListResponse
import com.ggumtle.domain.rest.repository.MissionRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GetMissionListUseCase @Inject constructor(
    private val missionRepository: MissionRepository
) {
    operator fun invoke(): Flow<Resource<MissionListResponse>> {
        return missionRepository.getMissionList()
    }
}