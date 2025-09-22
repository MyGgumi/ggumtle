package com.ggumtle.data.rest.remote.datasource

import com.ggumtle.data.rest.model.growth.response.EnhanceMonggingResponseDto
import com.ggumtle.data.rest.model.growth.response.MonggingDetailResponseDto
import com.ggumtle.data.rest.model.growth.response.MonggingListResponseDto
import com.ggumtle.data.rest.remote.service.GrowthService
import com.ggumtle.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class GrowthRemoteDataSource @Inject constructor(
    private val growthService: GrowthService
) {
    suspend fun getMonggingList(): NetworkResult<MonggingListResponseDto> =
        growthService.getMonggingList()

    suspend fun enhanceMongging(monggingId: Long): NetworkResult<EnhanceMonggingResponseDto> =
        growthService.enhanceMongging(monggingId)

    suspend fun getMonggingDetail(monggingId: Long): NetworkResult<MonggingDetailResponseDto> =
        growthService.getMonggingDetail(monggingId)
}