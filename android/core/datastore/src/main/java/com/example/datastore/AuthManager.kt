package com.example.datastore

import android.content.Context
import android.util.Base64
import android.util.Log
import com.example.common.network.di.ApplicationScope
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import org.json.JSONObject
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.text.isNullOrBlank
import kotlin.text.split
import kotlin.text.take

@Singleton
class AuthManager @Inject constructor(
    private val authDataStore: AuthDataStore,
    private val googleAuthManager: GoogleAuthManager,
    @ApplicationScope private val applicationScope: CoroutineScope
) {
    @Volatile
    private var cachedAccessToken: String? = null
    @Volatile
    private var cachedRefreshToken: String? = null
    @Volatile
    private var cachedUserEmail: String? = null

    private val _logoutEvent = MutableSharedFlow<LogoutReason>(extraBufferCapacity = 1)
    val logoutEvent: SharedFlow<LogoutReason> = _logoutEvent

    private val _autoLoginState = MutableStateFlow<AutoLoginState>(AutoLoginState.Loading)
    val autoLoginState: StateFlow<AutoLoginState> = _autoLoginState.asStateFlow()

    init {
        applicationScope.launch {
            launch {
                authDataStore.accessTokenFlow.collect { token ->
                    Log.d("AuthManager", "AccessToken 캐시 업데이트: ${token?.take(10)}...")
                    cachedAccessToken = token
                }
            }
            launch {
                authDataStore.refreshTokenFlow.collect { token ->
                    Log.d("AuthManager", "RefreshToken 캐시 업데이트: ${token?.take(10)}...")
                    cachedRefreshToken = token
                }
            }
            launch {
                authDataStore.userEmailFlow.collect { email ->
                    Log.d("AuthManager", "UserEmail 캐시 업데이트: $email")
                    cachedUserEmail = email
                }
            }
        }
    }

    // 기존 메서드들
    fun getAccessToken(): String? = cachedAccessToken
    fun getRefreshToken(): String? = cachedRefreshToken
    fun getUserEmail(): String? = cachedUserEmail

    suspend fun signInWithGoogle(activityContext: Context): GoogleSignInResult {
        Log.d("AuthManager", "Google 로그인 시작")

        return try {
            val idToken = googleAuthManager.signInWithGoogle(activityContext)
            if (idToken != null) {
                Log.d("AuthManager", "Google 로그인 성공, ID 토큰 획득")
                GoogleSignInResult.Success(idToken)
            } else {
                Log.d("AuthManager", "Google 로그인 실패 - ID 토큰이 null")
                GoogleSignInResult.Error(kotlin.Exception("ID 토큰을 받을 수 없습니다"))
            }
        } catch (e: Exception) {
            Log.e("AuthManager", "Google 로그인 중 오류 발생", e)
            GoogleSignInResult.Error(e)
        }
    }

    suspend fun signOutWithGoogle() {
        Log.d("AuthManager", "Google 로그아웃 시작")

        try {
            val googleLogoutSuccess = googleAuthManager.googleLogout()
            Log.d("AuthManager", "Google 로그아웃 결과: $googleLogoutSuccess")

            logout(LogoutReason.UserLogout)

        } catch (e: Exception) {
            Log.e("AuthManager", "Google 로그아웃 중 오류 발생", e)
            logout(LogoutReason.UserLogout)
        }
    }

    fun saveTokenAndEmail(accessToken: String, refreshToken: String, email: String) {
        Log.d("AuthManager", "토큰, 이메일 저장 시작")
        Log.d("AuthManager", "AccessToken: ${accessToken.take(10)}...")
        Log.d("AuthManager", "RefreshToken: ${refreshToken.take(10)}...")
        Log.d("AuthManager", "이메일 저장: $email")

        cachedAccessToken = accessToken
        cachedRefreshToken = refreshToken
        cachedUserEmail = email

        applicationScope.launch(Dispatchers.IO) {
            try {
                authDataStore.saveAccessToken(accessToken)
                authDataStore.saveRefreshToken(refreshToken)
                authDataStore.saveUserEmail(email)
                Log.d("AuthManager", "토큰, 이메일 저장 완료")
            } catch (e: Exception) {
                Log.e("AuthManager", "토큰, 이메일 저장 실패", e)
            }
        }
    }

    fun isLoggedIn(): Boolean = !cachedAccessToken.isNullOrBlank() && !cachedRefreshToken.isNullOrBlank()

    fun logout(reason: LogoutReason) {
        Log.d("AuthManager", "로그아웃 시작: $reason")

        cachedAccessToken = null
        cachedRefreshToken = null
        cachedUserEmail = null

        applicationScope.launch(Dispatchers.IO) {
            try {
                authDataStore.deleteAccessToken()
                authDataStore.deleteRefreshToken()
                authDataStore.deleteUserEmail()

                _logoutEvent.emit(reason)

                Log.d("AuthManager", "로그아웃 완료")
            } catch (e: Exception) {
                Log.e("AuthManager", "로그아웃 실패", e)
            }
        }
    }

    fun clearAll() {
        Log.d("AuthManager", "전체 데이터 클리어")
        logout(LogoutReason.UserLogout)
    }

    suspend fun checkAutoLogin() {
        Log.d("AuthManager", "자동 로그인 체크 시작")

        try {
            val accessToken = getAccessToken()
            val refreshToken = getRefreshToken()

            when {
                accessToken.isNullOrBlank() || refreshToken.isNullOrBlank() -> {
                    Log.d("AuthManager", "저장된 토큰 없음")
                    _autoLoginState.value = AutoLoginState.RequireLogin
                }

                isTokenValid(accessToken) || isTokenValid(refreshToken) -> {
                    Log.d("AuthManager", "AccessToken 유효 - 자동 로그인 성공")
                    _autoLoginState.value = AutoLoginState.Success
                }
                else -> {
                    Log.d("AuthManager", "모든 토큰 만료 - 로그인 필요")
                    logout(LogoutReason.TokenExpired)
                    _autoLoginState.value = AutoLoginState.RequireLogin
                }
            }
        } catch (e: Exception) {
            Log.e("AuthManager", "자동 로그인 체크 중 오류", e)
            _autoLoginState.value = AutoLoginState.RequireLogin
        }
    }

    private fun isTokenValid(token: String): Boolean {
        return try {
            val parts = token.split(".")
            if (parts.size != 3) return false

            val payload = String(Base64.decode(parts[1], Base64.URL_SAFE))
            val jsonObject = JSONObject(payload)
            val exp = jsonObject.getLong("exp")
            val currentTime = System.currentTimeMillis() / 1000

            exp > currentTime
        } catch (e: Exception) {
            Log.e("AuthManager", "토큰 유효성 검사 실패", e)
            false
        }
    }
}

sealed class AutoLoginState {
    object Loading : AutoLoginState()
    object Success : AutoLoginState()
    object RequireLogin : AutoLoginState()
}

/**
 * Google 로그인 결과
 */
sealed class GoogleSignInResult {
    data class Success(val idToken: String) : GoogleSignInResult()
    data class Error(val exception: Exception) : GoogleSignInResult()
    object Cancelled : GoogleSignInResult()
}