package com.ggumtle.domain.rest.repository

import com.ggumtle.domain.rest.model.growth.response.EnhanceMonggingResponse
import com.ggumtle.domain.rest.model.growth.response.MonggingDetailResponse
import com.ggumtle.domain.rest.model.growth.response.MonggingListResponse
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow

interface GrowthRepository {
    fun getMonggingList(): Flow<Resource<MonggingListResponse>>
    fun enhanceMongging(monggingId: Long): Flow<Resource<EnhanceMonggingResponse>>
    fun getMonggingDetail(monggingId: Long): Flow<Resource<MonggingDetailResponse>>
}