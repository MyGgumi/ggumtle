package com.example.auth

import com.example.designsystem.dialog.DialogState

object LoginContract {

    data class State(
        val isLoading: Boolean = false,
        val isLoginSuccess: Boolean = false,
        val isNavigating: Boolean = false,
        val dialogState: DialogState = DialogState.Hidden
    )
    sealed interface SideEffect {
        data object NavigateToMain : SideEffect
        data class ShowToast(val message: String) : SideEffect
    }

}