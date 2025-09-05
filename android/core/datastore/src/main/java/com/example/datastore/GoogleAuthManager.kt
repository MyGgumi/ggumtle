package com.example.datastore

import android.content.Context
import android.util.Log
import androidx.credentials.ClearCredentialStateRequest
import androidx.credentials.CredentialManager
import androidx.credentials.GetCredentialRequest
import androidx.credentials.exceptions.GetCredentialCancellationException
import androidx.credentials.exceptions.GetCredentialException
import androidx.credentials.exceptions.NoCredentialException
import com.ggumtle.core.datastore.BuildConfig
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import dagger.hilt.android.qualifiers.ActivityContext
import dagger.hilt.android.qualifiers.ApplicationContext
import javax.inject.Inject
import kotlin.onFailure
import kotlin.onSuccess
import kotlin.runCatching

class GoogleAuthManager @Inject constructor(
    @ApplicationContext private val context: Context
) {

    companion object {
        private const val TAG = "GoogleAuth"
    }

    private val credentialManager by lazy { CredentialManager.create(context) }

    private fun buildGoogleIdOption() = GetGoogleIdOption.Builder()
        .setServerClientId(BuildConfig.GOOGLE_CLIENT_ID)
        .setFilterByAuthorizedAccounts(false)
        .build()

    private fun buildGetCredentialRequest() = GetCredentialRequest.Builder()
        .addCredentialOption(buildGoogleIdOption())
        .build()

    suspend fun signInWithGoogle(activityContext: Context): String? = runCatching {
        val result = credentialManager.getCredential(
            request = buildGetCredentialRequest(),
            context = activityContext
        )
        Log.d(TAG, "Credential 받아옴: $result")

        val credential = result.credential

        Log.d(TAG, "Credential 데이터: ${credential.data}")

        val googleCredential = GoogleIdTokenCredential.createFrom(credential.data)

        googleCredential.idToken
    }
        .onFailure { exception ->
            when (exception) {
                is GetCredentialCancellationException -> {
                    Log.d(TAG, "사용자가 로그인을 취소했습니다")
                }

                is NoCredentialException -> {
                    Log.d("GoogleAuth", "클라이언트 ID: ${BuildConfig.GOOGLE_CLIENT_ID}")
                    Log.d(TAG, "사용 가능한 Google 계정이 없습니다")
                    Log.d(TAG, "${exception.message}")
                }

                is GetCredentialException -> {
                    Log.e(TAG, "Google 로그인 실패: ${BuildConfig.GOOGLE_CLIENT_ID}")
                    Log.e(TAG, "Google 로그인 실패: ${exception.message}")
                }

                else -> {
                    Log.e(TAG, "예상치 못한 오류: ${exception.message}")
                }
            }
        }
        .getOrNull()

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