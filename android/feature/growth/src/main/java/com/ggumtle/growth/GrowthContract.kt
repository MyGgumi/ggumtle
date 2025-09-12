package com.ggumtle.growth

import com.ggumtle.designsystem.dialog.DialogState

object GrowthContract {

    data class State(
        val isLoading: Boolean = false,
        val errorMessage: String? = null,
        val dialogState: DialogState = DialogState.Hidden
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data object NavigateToHome : SideEffect
    }
}