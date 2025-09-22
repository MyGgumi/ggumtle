package com.ggumtle.domain.rest.usecase.growth

import com.ggumtle.domain.rest.model.growth.response.MonggingDetailResponse
import com.ggumtle.domain.rest.repository.GrowthRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GetMonggingDetailUseCase @Inject constructor(
    private val growthRepository: GrowthRepository
) {
    operator fun invoke(monggingId: Long): Flow<Resource<MonggingDetailResponse>> {
        return growthRepository.getMonggingDetail(monggingId)
    }
}