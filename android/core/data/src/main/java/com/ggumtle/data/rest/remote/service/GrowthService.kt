package com.ggumtle.data.rest.remote.service

import com.ggumtle.data.rest.model.growth.response.EnhanceMonggingResponseDto
import com.ggumtle.data.rest.model.growth.response.MonggingDetailResponseDto
import com.ggumtle.data.rest.model.growth.response.MonggingListResponseDto
import com.ggumtle.network.rest.model.NetworkResult
import retrofit2.http.*

interface GrowthService {

    @GET("mongging")
    suspend fun getMonggingList(): NetworkResult<MonggingListResponseDto>

    @GET("mongging/{mongging-id}/detail")
    suspend fun getMonggingDetail(
        @Path("mongging-id") monggingId: Long
    ): NetworkResult<MonggingDetailResponseDto>

    @PATCH("mongging/{mongging-id}/enhance")
    suspend fun enhanceMongging(
        @Path("mongging-id") monggingId: Long
    ): NetworkResult<EnhanceMonggingResponseDto>

}