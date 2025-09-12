package com.ggumtle.domain.rest.repository

import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.model.auth.request.GoogleLoginRequest
import com.ggumtle.domain.rest.model.auth.response.LoginResponse
import kotlinx.coroutines.flow.Flow

interface AuthRepository {
      fun googleLogin(request: GoogleLoginRequest): Flow<Resource<LoginResponse>>
}