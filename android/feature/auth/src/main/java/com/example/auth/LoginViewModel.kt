package com.example.auth

import android.content.Context
import android.util.Log
import androidx.lifecycle.ViewModel
import com.example.datastore.AuthManager
import com.example.datastore.GoogleSignInResult
import com.example.designsystem.dialog.DialogState
import com.example.domain.rest.model.Resource
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.model.UnityMethod
import com.example.domain.unity.model.UnityTarget
import com.example.domain.rest.usecase.auth.GoogleLoginUseCase
import com.example.domain.websocket.usecase.connection.ConnectWebSocketUseCase
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class LoginViewModel @Inject constructor(
    private val googleLoginUseCase: GoogleLoginUseCase,
    private val authManager: AuthManager,
    private val connectWebSocketUseCase: ConnectWebSocketUseCase,
    private val unitySendManager: UnitySendManager,
    ) : ViewModel(), ContainerHost<LoginContract.State, LoginContract.SideEffect> {

    override val container = container<LoginContract.State, LoginContract.SideEffect>(
        initialState = LoginContract.State()
    )

    fun performGoogleLogin(activityContext: Context) = intent {
        try {
            when (val googleResult = authManager.signInWithGoogle(activityContext)) {

                is GoogleSignInResult.Success -> {

                    googleLoginUseCase.invoke(googleResult.idToken).collect { resource ->
                        when (resource) {
                            is Resource.Loading -> reduce { state.copy(isLoading = true) }

                            is Resource.Success -> {
                                reduce {
                                    state.copy(
                                        isLoading = false,
                                        isLoginSuccess = true
                                    )
                                }
                            }

                            is Resource.Failure -> {
                                Log.d("performGoogleLogin", "performGoogleLogin: ${resource.errorMessage}")
                                reduce {
                                    state.copy(
                                        isLoading = false,
                                        dialogState = DialogState.SingleButtonDialog(
                                            content = "Google 로그인에 실패했습니다."
                                        )
                                    )
                                }
                            }
                        }
                    }
                }

                is GoogleSignInResult.Error -> {
                    reduce {
                        state.copy(
                            isLoading = false,
                            dialogState = DialogState.SingleButtonDialog(
                                content = "Google 로그인에 실패했습니다."
                            )
                        )
                    }
                }

                is GoogleSignInResult.Cancelled -> reduce { state.copy(isLoading = false) }

            }
        } catch (e: Exception) {
            state.copy(
                isLoading = false,
                dialogState = DialogState.SingleButtonDialog(
                    content = "예상치 못한 오류가 발생했습니다."
                )
            )
        }
    }

    fun navigateToMain() = intent {
        Log.d("LoginViewModel", "navigateToMain 호출됨")
        reduce {
            state.copy(
                isNavigating = true,
                isLoading = false
            )
        }
        connectWebSocketUseCase.invoke()
        Log.d("LoginViewModel", "START_TRANSITION 유니티로 전송")
        unitySendManager.sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.START_TRANSITION.value
        )
        delay(2000)
        Log.d("LoginViewModel", "메인으로 네비게이션 시작")
        postSideEffect(LoginContract.SideEffect.NavigateToMain)
    }

    fun setLoginStatus(isLoggedIn: Boolean) = intent {
        reduce { state.copy(isLoginSuccess = isLoggedIn) }
    }

    fun hideDialog() = intent { reduce { state.copy(dialogState = DialogState.Hidden) } }
}