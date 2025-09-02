package com.example.network.rest.util.authenticator

import com.example.network.rest.util.authenticator.model.request.RefreshReissueRequest
import com.example.network.rest.util.authenticator.model.response.RefreshTokenResponse
import retrofit2.Call
import retrofit2.http.Body
import retrofit2.http.POST

interface TokenRefreshService {
    @POST("auth/reissue")
    fun reissue(
        @Body request: RefreshReissueRequest
    ): Call<RefreshTokenResponse>
}