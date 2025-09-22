package com.ggumtle.data.rest.repository

import com.ggumtle.data.rest.model.growth.response.toDomain
import com.ggumtle.data.rest.remote.datasource.GrowthRemoteDataSource
import com.ggumtle.data.rest.util.executeApiCall
import com.ggumtle.domain.rest.model.growth.response.EnhanceMonggingResponse
import com.ggumtle.domain.rest.model.growth.response.MonggingDetailResponse
import com.ggumtle.domain.rest.model.growth.response.MonggingListResponse
import com.ggumtle.domain.rest.repository.GrowthRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GrowthRepositoryImpl @Inject constructor(
    private val remoteDataSource: GrowthRemoteDataSource
) : GrowthRepository {

    override fun getMonggingList(): Flow<Resource<MonggingListResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getMonggingList() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "몽깅이 목록 조회에 실패했습니다"
        )

    override fun enhanceMongging(monggingId: Long): Flow<Resource<EnhanceMonggingResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.enhanceMongging(monggingId) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "몽깅이 강화에 실패했습니다"
        )

    override fun getMonggingDetail(monggingId: Long): Flow<Resource<MonggingDetailResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getMonggingDetail(monggingId) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "몽깅이 상세 조회에 실패했습니다"
        )
}