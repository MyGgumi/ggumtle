package com.example.data.remote.datasource

import com.example.data.model.auth.request.GoogleLoginRequestDto
import com.example.data.model.auth.response.LoginResponseDto
import com.example.data.remote.service.AuthService
import com.example.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRemoteDataSource @Inject constructor(
    private val authService: AuthService
) {
    suspend fun googleLogin(request: GoogleLoginRequestDto): NetworkResult<LoginResponseDto> =
        authService.googleLogin(request)
}