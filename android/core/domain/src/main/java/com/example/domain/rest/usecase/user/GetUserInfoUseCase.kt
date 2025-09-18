package com.example.domain.rest.usecase.user

import com.example.domain.rest.model.user.response.UserInfoResponse
import com.example.domain.rest.repository.UserRepository
import com.ggumtle.domain.rest.model.Resource
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class GetUserInfoUseCase @Inject constructor(
    private val userRepository: UserRepository
) {
    operator fun invoke(): Flow<Resource<UserInfoResponse>> {
        return userRepository.getUserInfo()
    }
}