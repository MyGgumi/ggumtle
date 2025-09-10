package com.ggumtle.startup

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.designsystem.dialog.DialogState
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.UnityStartupManager
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class StartUpViewModel @Inject constructor(
    private val unityStartupManager: UnityStartupManager
) : ContainerHost<StartUpContract.State, StartUpContract.SideEffect>, ViewModel() {

    override val container =
        container<StartUpContract.State, StartUpContract.SideEffect>(initialState = StartUpContract.State())

    init {
        observeStartupManager()
        startTimeoutTimer()
    }

    private fun observeStartupManager() {
        combine(
            unityStartupManager.progressFlow,
            unityStartupManager.messageFlow
        ) { progress, message -> updateProgress(progress, message)
        }.launchIn(viewModelScope)

        unityStartupManager.completionFlow
            .onEach { onLoadingComplete() }
            .launchIn(viewModelScope)

        unityStartupManager.errorFlow
            .onEach { errorMessage -> onLoadingError(errorMessage) }
            .launchIn(viewModelScope)
    }

    private fun updateProgress(progress: Int, message: String) = intent {
        reduce {
            state.copy(
                progress = progress,
                loadingMessage = message,
                isError = false,
                isLoading = progress < 100
            )
        }
    }

    private fun onLoadingComplete() = intent {
        reduce {
            state.copy(
                isLoading = false,
                progress = 100,
                loadingMessage = "완료!"
            )
        }
        postSideEffect(StartUpContract.SideEffect.NavigateToLogin)
    }

    private fun onLoadingError(errorMessage: String) = intent {
        reduce {
            state.copy(
                isLoading = false,
                isError = true,
                errorMessage = errorMessage
            )
        }
    }

    private fun startTimeoutTimer() = intent {
        delay(30_0000)
        if (state.isLoading && !state.isError) {
            reduce {
                state.copy(
                    isLoading = false,
                    isError = true,
                    errorMessage = "로딩 시간이 초과되었습니다",
                    dialogState = DialogState.SingleButtonDialog(
                        content = "로딩 시간이 초과되었습니다. 앱을 다시 시작해주세요.",
                        onConfirm = { exitApp() }
                    )
                )
            }
        }
    }

    private fun exitApp() = intent { postSideEffect(StartUpContract.SideEffect.ExitApp) }

    fun hideDialog() = intent { reduce { state.copy(dialogState = DialogState.Hidden) } }
}