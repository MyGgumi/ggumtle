package com.ggumtle.data.rest.remote.datasource

import com.ggumtle.data.rest.model.auth.request.GoogleLoginRequestDto
import com.ggumtle.data.rest.model.auth.response.LoginResponseDto
import com.ggumtle.data.rest.remote.service.AuthService
import com.ggumtle.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class AuthRemoteDataSource @Inject constructor(
    private val authService: AuthService
) {
    suspend fun googleLogin(request: GoogleLoginRequestDto): NetworkResult<LoginResponseDto> =
        authService.googleLogin(request)
}