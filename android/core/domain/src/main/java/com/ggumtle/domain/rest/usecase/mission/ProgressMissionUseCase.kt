package com.ggumtle.domain.rest.usecase.mission

import com.ggumtle.domain.rest.model.mission.response.MissionProgressResponse
import com.ggumtle.domain.rest.repository.MissionRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class ProgressMissionUseCase @Inject constructor(
    private val missionRepository: MissionRepository
) {
    operator fun invoke(memberMissionId: Long): Flow<Resource<MissionProgressResponse>> {
        return missionRepository.progressMission(memberMissionId)
    }
}