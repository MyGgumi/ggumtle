package com.example.datastore

import android.content.Context
import android.util.Log
import androidx.credentials.ClearCredentialStateRequest
import androidx.credentials.CredentialManager
import androidx.credentials.GetCredentialRequest
import androidx.credentials.exceptions.GetCredentialCancellationException
import androidx.credentials.exceptions.GetCredentialException
import androidx.credentials.exceptions.NoCredentialException
import com.example.multimodulebase.core.datastore.BuildConfig
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import dagger.hilt.android.qualifiers.ApplicationContext
import javax.inject.Inject
import kotlin.onFailure
import kotlin.onSuccess
import kotlin.runCatching

/**
 * Google 로그인을 담당하는 클래스
 * Credential Manager API를 사용해서 Google One-Tap 로그인 처리
 */
class GoogleAuthManager @Inject constructor(
    @ApplicationContext private val context: Context
) {

    companion object {
        private const val TAG = "GoogleAuth"
    }

    // CredentialManager: Android의 새로운 인증 API (Google 로그인, 패스워드 등)
    private val credentialManager by lazy { CredentialManager.create(context) }

    /**
     * Google ID 옵션 설정
     * - serverClientId: 백엔드에서 ID 토큰을 검증할 때 사용하는 클라이언트 ID
     * - filterByAuthorizedAccounts: false = 모든 Google 계정 표시
     */
    private fun buildGoogleIdOption() = GetGoogleIdOption.Builder()
        .setServerClientId(BuildConfig.GOOGLE_CLIENT_ID)
        .setFilterByAuthorizedAccounts(false)
        .build()

    /**
     * 인증 요청 객체 생성
     * Google ID 옵션을 포함한 전체 요청
     */
    private fun buildGetCredentialRequest() = GetCredentialRequest.Builder()
        .addCredentialOption(buildGoogleIdOption())
        .build()

    /**
     * Google One‐Tap 로그인 시도
     * @return 성공시 ID 토큰(JWT), 실패시 null
     */
    suspend fun signInWithGoogle(activityContext: Context): String? = runCatching {
        // 1. Google 로그인 요청
        val result = credentialManager.getCredential(
            request = buildGetCredentialRequest(),
            context = activityContext
        )
        Log.d(TAG, "Credential 받아옴: $result")

        // 2. Credential = Google에서 받은 인증 정보 (ID 토큰, 이메일 등)
        val credential = result.credential
        Log.d(TAG, "Credential 데이터: ${credential.data}")

        // 3. GoogleIdTokenCredential로 변환해서 ID 토큰 추출
        val googleCredential = GoogleIdTokenCredential.createFrom(credential.data)

        // 4. ID 토큰 반환 (JWT 형태의 문자열)
        googleCredential.idToken
    }
        .onFailure { exception ->
            when (exception) {
                is GetCredentialCancellationException -> {
                    Log.d(TAG, "사용자가 로그인을 취소했습니다")
                }

                is NoCredentialException -> {
                    Log.d(TAG, "사용 가능한 Google 계정이 없습니다 : ${exception.message}")
                }

                is GetCredentialException -> {
                    Log.e(TAG, "Google 로그인 실패: ${exception.message}")
                }

                else -> {
                    Log.e(TAG, "예상치 못한 오류: ${exception.message}")
                }
            }
        }
        .getOrNull()

    /**
     * Google 로그아웃
     * 저장된 인증 정보를 모두 삭제
     */
    suspend fun googleLogout(): Boolean {
        return runCatching {
            credentialManager.clearCredentialState(
                ClearCredentialStateRequest()
            )
        }
            .onSuccess { Log.d(TAG, "Google 로그아웃 성공") }
            .onFailure { Log.e(TAG, "Google 로그아웃 실패: ${it.message}") }
            .isSuccess
    }
}