package com.ggumtle.network.rest.util.interceptor

import android.util.Log
import com.ggumtle.datastore.AuthManager
import jakarta.inject.Inject
import okhttp3.Interceptor
import okhttp3.Response

class AuthInterceptor @Inject constructor(
    private val authManager: AuthManager
) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val originalRequest = chain.request()

        // 인증이 필요 없는 경로는 그대로 진행
        if (shouldSkipAuth(originalRequest.url.encodedPath)) {
            return chain.proceed(originalRequest)
        }

        val accessToken = authManager.getAccessToken()
        Log.d("Authorization1", "intercept: $accessToken")
        val request = if (!accessToken.isNullOrEmpty()) {
            Log.d("Authorization2", "intercept: $accessToken")

            originalRequest.newBuilder()
                .header("Authorization", "Bearer $accessToken")
                .build()
        } else {
            originalRequest
        }

        // 요청 실행
        return chain.proceed(request)
    }

    private fun shouldSkipAuth(path: String): Boolean {
        val skip = path.startsWith("/auth/login") || path.startsWith("/auth/register")
        Log.d("AuthInterceptor", "Path: $path, skipAuth: $skip")
        return skip
    }
}