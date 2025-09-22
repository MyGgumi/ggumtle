package com.ggumtle.domain.rest.usecase.auth

import com.ggumtle.domain.rest.model.auth.response.LogoutResponse
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.repository.AuthRepository
import kotlinx.coroutines.flow.Flow
import javax.inject.Inject

class LogoutUseCase @Inject constructor(
    private val authRepository: AuthRepository
) {
    operator fun invoke(): Flow<Resource<LogoutResponse>> {
        return authRepository.logout()
    }
}