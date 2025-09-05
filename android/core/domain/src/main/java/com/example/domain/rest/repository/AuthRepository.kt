package com.example.domain.rest.repository

import com.example.domain.rest.model.Resource
import com.example.domain.rest.model.auth.request.GoogleLoginRequest
import com.example.domain.rest.model.auth.response.LoginResponse
import kotlinx.coroutines.flow.Flow

interface AuthRepository {
      fun googleLogin(request: GoogleLoginRequest): Flow<Resource<LoginResponse>>
}