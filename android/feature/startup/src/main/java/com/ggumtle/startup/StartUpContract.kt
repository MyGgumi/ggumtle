package com.ggumtle.startup

import com.example.designsystem.dialog.DialogState

object StartUpContract {
    data class State(
        val isLoading: Boolean = true,
        val progress: Int = 0,
        val loadingMessage: String = "Unity 초기화 중...",
        val isError: Boolean = false,
        val errorMessage: String = "",
        val dialogState: DialogState = DialogState.Hidden
    )

    sealed class SideEffect {
        object NavigateToLogin : SideEffect()
        object ExitApp : SideEffect()
        data class ShowToast(val message: String) : SideEffect()
    }
}