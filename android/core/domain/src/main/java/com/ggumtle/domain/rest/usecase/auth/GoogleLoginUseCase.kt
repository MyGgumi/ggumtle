package com.ggumtle.domain.rest.usecase.auth

import android.util.Log
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.model.auth.request.GoogleLoginRequest
import com.ggumtle.domain.rest.model.auth.response.LoginResponse
import com.ggumtle.domain.rest.repository.AuthRepository
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.onEach
import javax.inject.Inject

class GoogleLoginUseCase @Inject constructor(
    private val authRepository: AuthRepository,
    private val authManager: AuthManager
) {
    operator fun invoke(idToken: String): Flow<Resource<LoginResponse>> {

        if (idToken.isBlank()) {
            return flow { emit(Resource.Failure(errorMessage = "Google 인증에 실패했습니다", code = 400)) }
        }

        val request = GoogleLoginRequest(idToken = idToken)
        Log.d("GoogleLoginUseCase", "GoogleLoginRequest 생성: $request")

        return authRepository.googleLogin(request)
            .onEach { resource ->
                Log.d("GoogleLoginUseCase", "Repository 응답: $resource")
                when (resource) {
                    is Resource.Loading -> {
                        Log.d("GoogleLoginUseCase", "백엔드 API 호출 중...")
                    }

                    is Resource.Success -> {
                        Log.d("GoogleLoginUseCase", "백엔드 API 성공, 토큰 저장 중...")
                        val loginResponse = resource.data
                        authManager.saveTokenAndEmail(
                            accessToken = loginResponse.accessToken,
                            memberId = loginResponse.memberId
                        )
                    }

                    is Resource.Failure -> {
                        Log.e(
                            "GoogleLoginUseCase",
                            "백엔드 API 실패: ${resource.errorMessage}, 코드: ${resource.code}"
                        )
                    }
                }
            }
    }
}