package com.ggumtle.mission

object MissionContract {
    data class State(
        val isLoading: Boolean = false,
        val error: String? = null,
    )

    sealed class SideEffect {
        data class ShowToast(val message: String) : SideEffect()
        data object NavigateToGrowth : SideEffect()
        data class ShowError(val message: String) : SideEffect()
    }
}