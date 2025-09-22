package com.ggumtle.domain.rest.usecase.growth

import com.ggumtle.domain.rest.model.growth.response.MonggingListResponse
import com.ggumtle.domain.rest.repository.GrowthRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GetMonggingListUseCase @Inject constructor(
    private val growthRepository: GrowthRepository
) {
    operator fun invoke(): Flow<Resource<MonggingListResponse>> {
        return growthRepository.getMonggingList()
    }
}