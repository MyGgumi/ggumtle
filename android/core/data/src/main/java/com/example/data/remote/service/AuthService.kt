package com.example.data.remote.service

import com.example.data.model.auth.request.GoogleLoginRequestDto
import com.example.data.model.auth.response.LoginResponseDto
import com.example.network.rest.model.NetworkResult
import retrofit2.http.*

interface AuthService {

    @POST("auth/google-login")
    suspend fun googleLogin(
        @Body request: GoogleLoginRequestDto
    ): NetworkResult<LoginResponseDto>

}