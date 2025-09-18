package com.example.data.rest.remote.datasource

import com.example.data.rest.model.user.response.UserInfoResponseDto
import com.example.data.rest.remote.service.UserService
import com.ggumtle.network.rest.model.NetworkResult
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UserRemoteDataSource @Inject constructor(
    private val userService: UserService
) {
    suspend fun getUserInfo(): NetworkResult<UserInfoResponseDto> = userService.getUserInfo()
}