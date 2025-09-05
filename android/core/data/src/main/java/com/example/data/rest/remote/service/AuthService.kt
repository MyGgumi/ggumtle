package com.example.data.rest.remote.service

import com.example.data.rest.model.auth.request.GoogleLoginRequestDto
import com.example.data.rest.model.auth.response.LoginResponseDto
import com.example.network.rest.model.NetworkResult
import retrofit2.http.*

interface AuthService {

    @POST("auth/login")
    suspend fun googleLogin(
        @Body request: GoogleLoginRequestDto
    ): NetworkResult<LoginResponseDto>

}