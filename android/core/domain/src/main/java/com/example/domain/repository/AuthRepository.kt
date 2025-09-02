package com.example.domain.repository

import com.example.domain.model.Resource
import com.example.domain.model.auth.request.GoogleLoginRequest
import com.example.domain.model.auth.response.LoginResponse
import kotlinx.coroutines.flow.Flow

interface AuthRepository {
      fun googleLogin(request: GoogleLoginRequest): Flow<Resource<LoginResponse>>
}