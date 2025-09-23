package com.ggumtle.mission

import com.ggumtle.mission.model.BeforeSuccessMission

object MissionContract {
    data class State(
        val isLoading: Boolean = false,
        val error: String? = null,

        // 모델 배치 가능
        val isModelPlacementReady: Boolean = false,
        // 최근 캡처된 사진
        val lastCapturedImageUri: String? = null,
        // 최근 캡처된 사진에 AR 캐릭터 포함 유무
        val hasModelInLastImage: Boolean = false,
        // 캡처 진행중 상태
        val isCapturing: Boolean = false,
        val showImageDialog: Boolean = false,

        // 먹이주기 관련 상태
        val isDraggingFood: Boolean = false,
        val foodPosition: Pair<Float, Float>? = null,
        val feedingAnimationVisible: Boolean = false,

        // 미완료 미션
        val beforeSuccessMissions: List<BeforeSuccessMission> = emptyList()
    )

    sealed class SideEffect {
        data class ShowToast(val message: String) : SideEffect()
        data object NavigateToGrowth : SideEffect()
        data class ShowError(val message: String) : SideEffect()
    }
}