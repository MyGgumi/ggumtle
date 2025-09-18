package com.example.domain.rest.repository

import com.example.domain.rest.model.user.response.UserInfoResponse
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow

interface UserRepository {
    fun getUserInfo(): Flow<Resource<UserInfoResponse>>
}