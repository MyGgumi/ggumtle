package com.example.network.rest.util.authenticator

import android.util.Log
import com.example.datastore.AuthManager
import com.example.datastore.LogoutReason
import com.example.network.rest.util.authenticator.model.request.RefreshReissueRequest
import kotlinx.coroutines.runBlocking
import okhttp3.Authenticator
import okhttp3.Request
import okhttp3.Response
import okhttp3.Route
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class TokenAuthenticator @Inject constructor(
    private val authManager: AuthManager,
    private val tokenRefreshService: TokenRefreshService
) : Authenticator {

    companion object {
        private const val TAG = "TokenAuthenticator"
        private const val MAX_RETRY_COUNT = 3
    }

    override fun authenticate(route: Route?, response: Response): Request? {
        return try {
            Log.d(TAG, "401 응답으로 인한 authenticate 호출")

            // 무한루프 방지: 재시도 횟수 확인
            val retryCount = response.request.header("X-Retry-Count")?.toIntOrNull() ?: 0
            if (retryCount >= MAX_RETRY_COUNT) {
                Log.w(TAG, "최대 재시도 횟수($MAX_RETRY_COUNT) 초과, 로그아웃")
                throw Exception("토큰 재시도 횟수 초과")
            }

            // runBlocking으로 코루틴 처리 (Authenticator는 동기 인터페이스)
            runBlocking {
                val currentAccessToken = authManager.getAccessToken()
                    ?: throw Exception("accessToken is null")
                val currentRefreshToken = authManager.getRefreshToken()
                    ?: throw Exception("refreshToken is null")

                Log.d(TAG, "현재 accessToken: ${currentAccessToken.take(10)}...")
                Log.d(TAG, "현재 refreshToken: ${currentRefreshToken.take(10)}...")

                synchronized(this@TokenAuthenticator) {
                    val requestToken = response.request.header("Authorization")?.removePrefix("Bearer ")
                    if (currentAccessToken != requestToken) {
                        Log.d(TAG, "토큰이 이미 갱신되었으므로 현재 토큰으로 요청 재시도")
                        return@runBlocking response.request.createRequestWithRenewedToken(currentAccessToken, retryCount + 1)
                    }

                    // 토큰 갱신 시도
                    Log.d(TAG, "토큰 갱신 시작")
                    val call = tokenRefreshService.reissue(
                        RefreshReissueRequest(refreshToken = currentRefreshToken)
                    )
                    val reissueResponse = call.execute()

                    if (reissueResponse.isSuccessful) {
                        val newTokenData = reissueResponse.body()
                        if (newTokenData != null) {
                            Log.d(TAG, "토큰 갱신 성공")

                            // 새로운 토큰 저장
                            authManager.saveTokenAndEmail(
                                accessToken = newTokenData.accessToken,
                                refreshToken = newTokenData.refreshToken,
                                email = newTokenData.email
                            )

                            Log.d(TAG, "갱신된 accessToken: ${newTokenData.accessToken.take(10)}...")
                            Log.d(TAG, "갱신된 refreshToken: ${newTokenData.refreshToken.take(10)}...")

                            return@runBlocking response.request.createRequestWithRenewedToken(newTokenData.accessToken, retryCount + 1)
                        } else {
                            Log.e(TAG, "토큰 갱신 응답이 null")
                            throw Exception("토큰 갱신 응답이 null")
                        }
                    } else {
                        Log.e(TAG, "토큰 갱신 실패: ${reissueResponse.code()} - ${reissueResponse.message()}")
                        throw Exception("토큰 갱신 실패: ${reissueResponse.code()}")
                    }
                }
            }

        } catch (e: Exception) {
            Log.e(TAG, "토큰 인증 실패", e)
            runBlocking {
                authManager.logout(LogoutReason.TokenExpired)
            }
            null
        }
    }

    private fun Request.createRequestWithRenewedToken(renewedToken: String, retryCount: Int = 0): Request =
        newBuilder()
            .header("Authorization", "Bearer $renewedToken")
            .header("X-Retry-Count", retryCount.toString())
            .build()
}