package com.ggumtle.data.rest.repository

import android.util.Log
import com.ggumtle.common.constant.HttpStatus
import com.ggumtle.data.rest.model.auth.request.toDto
import com.ggumtle.data.rest.model.auth.response.toDomain
import com.ggumtle.data.rest.remote.datasource.AuthRemoteDataSource
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.model.auth.request.GoogleLoginRequest
import com.ggumtle.domain.rest.model.auth.response.LoginResponse
import com.ggumtle.domain.rest.repository.AuthRepository
import com.ggumtle.network.rest.model.NetworkResult
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.flowOn
import javax.inject.Inject

class AuthRepositoryImpl @Inject constructor(
    private val remoteDataSource: AuthRemoteDataSource
) : AuthRepository {

    override fun googleLogin(request: GoogleLoginRequest): Flow<Resource<LoginResponse>> = flow {
        Log.d("AuthRepository", "백엔드 Google 로그인 API 호출 시작: $request")
        emit(Resource.Loading)

        when (val result = remoteDataSource.googleLogin(request.toDto())) {
            is NetworkResult.Success -> {
                Log.d("AuthRepository", "API 성공: ${result.data}")
                emit(Resource.Success(result.data.toDomain()))
            }

            is NetworkResult.Error -> {
                Log.e("AuthRepository", "API 에러: ${result.message}, 코드: ${result.code}")
                emit(
                    Resource.Failure(result.message ?: "Google 로그인에 실패했습니다", result.code)
                )
            }

            is NetworkResult.Exception -> {
                Log.e("AuthRepository", "네트워크 예외: ${result.e.message}")
                emit(
                    Resource.Failure(
                        result.e.message ?: "네트워크 오류가 발생했습니다", HttpStatus.INTERNAL_SERVER_ERROR
                    )
                )
            }
        }
    }.flowOn(Dispatchers.IO)
}