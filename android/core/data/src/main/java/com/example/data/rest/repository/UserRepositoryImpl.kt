package com.example.data.rest.repository

import com.example.data.rest.model.user.response.toDomain
import com.example.data.rest.remote.datasource.UserRemoteDataSource
import com.example.data.rest.util.executeApiCall
import com.example.domain.rest.model.user.response.UserInfoResponse
import com.example.domain.rest.repository.UserRepository
import com.ggumtle.common.constant.HttpStatus
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.network.rest.model.NetworkResult
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import javax.inject.Inject

class UserRepositoryImpl @Inject constructor(
    private val remoteDataSource: UserRemoteDataSource
) : UserRepository {

    override fun getUserInfo(): Flow<Resource<UserInfoResponse>> =
        executeApiCall(
            apiCall = { remoteDataSource.getUserInfo() },
            mapper = { it.toDomain() },
            defaultErrorMessage = "사용자 정보 조회에 실패했습니다"
        )
}