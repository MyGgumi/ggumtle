package com.ggumtle.data.rest.repository

import com.ggumtle.data.rest.util.executeApiCall
import com.ggumtle.domain.rest.model.auth.response.LogoutResponse
import com.ggumtle.data.rest.model.auth.request.toDto
import com.ggumtle.data.rest.model.auth.response.toDomain
import com.ggumtle.data.rest.remote.datasource.AuthRemoteDataSource
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.model.auth.request.GoogleLoginRequest
import com.ggumtle.domain.rest.model.auth.response.LoginResponse
import com.ggumtle.domain.rest.repository.AuthRepository
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val remoteDataSource: AuthRemoteDataSource
) : AuthRepository {

    override fun googleLogin(request: GoogleLoginRequest): Flow<Resource<LoginResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.googleLogin(request.toDto()) },
            mapper = { it.toDomain() },
            defaultErrorMessage = "Google 로그인에 실패했습니다"
        )

    override fun logout(): Flow<Resource<LogoutResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.logout() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "로그아웃에 실패했습니다"
        )
}