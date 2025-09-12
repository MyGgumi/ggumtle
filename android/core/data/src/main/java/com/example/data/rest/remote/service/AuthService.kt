package com.ggumtle.data.rest.remote.service

import com.ggumtle.data.rest.model.auth.request.GoogleLoginRequestDto
import com.ggumtle.data.rest.model.auth.response.LoginResponseDto
import com.ggumtle.network.rest.model.NetworkResult
import retrofit2.http.*

interface AuthService {

    @POST("auth/login")
    suspend fun googleLogin(
        @Body request: GoogleLoginRequestDto
    ): NetworkResult<LoginResponseDto>

}