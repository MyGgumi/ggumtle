package com.ggumtle.datastore

import android.content.Context
import android.util.Base64
import android.util.Log
import com.ggumtle.common.network.di.ApplicationScope
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
    private var cachedMemberId: Long? = null
    @Volatile
    private var cachedAccessToken: String? = null
//    @Volatile
//    private var cachedRefreshToken: String? = null
//    @Volatile
//    private var cachedUserEmail: String? = null

    private val _logoutEvent = MutableSharedFlow<LogoutReason>(extraBufferCapacity = 1)
    val logoutEvent: SharedFlow<LogoutReason> = _logoutEvent

    private val _autoLoginState = MutableStateFlow<AutoLoginState>(AutoLoginState.Loading)
    val autoLoginState: StateFlow<AutoLoginState> = _autoLoginState.asStateFlow()

    init {
        applicationScope.launch {
            launch { authDataStore.accessTokenFlow.collect { token -> cachedAccessToken = token } }
            launch { authDataStore.memberIdFlow.collect { memberId -> cachedMemberId = memberId?.toLong() } }
//            launch { authDataStore.refreshTokenFlow.collect { token -> cachedRefreshToken = token } }
//            launch { authDataStore.userEmailFlow.collect { email -> cachedUserEmail = email } }
        }
    }

    // 기존 메서드들
    fun getAccessToken(): String? = cachedAccessToken
    fun getMemberId() : Long? = cachedMemberId
//    fun getRefreshToken(): String? = cachedRefreshToken
//    fun getUserEmail(): String? = cachedUserEmail

    suspend fun signInWithGoogle(activityContext: Context): GoogleSignInResult {
        return try {
            val idToken = googleAuthManager.signInWithGoogle(activityContext)
            if (idToken != null) {
                GoogleSignInResult.Success(idToken)
            } else {
                GoogleSignInResult.Error(kotlin.Exception("ID 토큰을 받을 수 없습니다"))
            }
        } catch (e: Exception) {
            GoogleSignInResult.Error(e)
        }
    }

    private suspend fun signOutWithGoogle() {
        try {
            googleAuthManager.googleLogout()
        } catch (e: Exception) {
            Log.e("AuthManager", "Google 로그아웃 실패", e)
        }
    }

    fun saveTokenAndEmail(accessToken: String, memberId: Long) {
        cachedAccessToken = accessToken
        cachedMemberId = memberId
//        cachedRefreshToken = refreshToken
//        cachedUserEmail = email

        applicationScope.launch(Dispatchers.IO) {
            try {
                authDataStore.saveAccessToken(accessToken)
                authDataStore.saveMemberId(memberId)
//                authDataStore.saveRefreshToken(refreshToken)
//                authDataStore.saveUserEmail(email)
            } catch (e: Exception) {
                Log.e("AuthManager", "토큰, 이메일 저장 실패", e)
            }
        }
    }

    fun isLoggedIn(): Boolean = !cachedAccessToken.isNullOrBlank() && !cachedMemberId.toString().isBlank()

    fun logout(reason: LogoutReason) {
        cachedAccessToken = null
        cachedMemberId = null
//        cachedRefreshToken = null
//        cachedUserEmail = null

        applicationScope.launch(Dispatchers.IO) {
            try {
                authDataStore.deleteAccessToken()
                authDataStore.deleteMemberId()
                signOutWithGoogle()
//                authDataStore.deleteRefreshToken()
//                authDataStore.deleteUserEmail()

                _logoutEvent.emit(reason)
            } catch (e: Exception) {
                Log.e("AuthManager", "로그아웃 실패", e)
            }
        }
    }

    fun clearAll() {
        logout(LogoutReason.UserLogout)
    }

    fun checkAutoLogin() {
        try {
            val accessToken = getAccessToken()

            val memberId = getMemberId()
//            val refreshToken = getRefreshToken()

            when {
                accessToken.isNullOrBlank() || memberId.toString().isBlank() -> {
                    _autoLoginState.value = AutoLoginState.RequireLogin
                }

                isTokenValid(accessToken) -> {
                    _autoLoginState.value = AutoLoginState.Success
                }
                else -> {
                    logout(LogoutReason.TokenExpired)
                    _autoLoginState.value = AutoLoginState.RequireLogin
                }
            }
        } catch (e: Exception) {
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