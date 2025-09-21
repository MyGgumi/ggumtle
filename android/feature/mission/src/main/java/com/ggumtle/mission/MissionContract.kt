package com.ggumtle.mission

object MissionContract {
    data class State(
        val isLoading: Boolean = false,  // 초기에는 로딩 상태가 아님
        val isArSessionReady: Boolean = false,
        val errorMessage: String? = null,
    )

    sealed class SideEffect {
        data class ShowToast(val message: String) : SideEffect()
        data class ShowPermissionDialog(val message: String) : SideEffect()
        data class ShowOpenGlNotSupportedDialog(val message: String) : SideEffect()
        data object RequestCameraPermission : SideEffect()
        data object NavigateToGrowth : SideEffect()
    }
}