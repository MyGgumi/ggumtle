package com.example.data.rest.remote.service

import com.example.data.rest.model.user.response.UserInfoResponseDto
import com.ggumtle.network.rest.model.NetworkResult
import retrofit2.http.*

interface UserService {

    @GET("user/info")
    suspend fun getUserInfo(): NetworkResult<UserInfoResponseDto>

}