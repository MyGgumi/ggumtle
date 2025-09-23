package com.ggumtle.growth

import com.ggumtle.designsystem.dialog.DialogState
import com.ggumtle.growth.model.CharacterInfo
import com.ggumtle.growth.model.DailyMission

object GrowthContract {

    data class State(

        // 드림 코인 개수
        val dreamCoin: Int = 0,

        // 몽깅이 정보
        val myCharacters: List<CharacterInfo> = emptyList(),

        // 현재 선택된 캐릭터
        val selectedCharacterIndex: Int = 0,

        // 미션 정보
        val dailyMission: DailyMission = DailyMission(
            totalMissionsCount = 0,
            completedMissionsCount = 0,
            remainingMissionsCount = 0,
            afterRewordMissionsCount = 0,
            missions = emptyList()
        ),

        val isShowingEnhanceFailure: Boolean = false,
        val isShowingEnhanceSuccess: Boolean = false,
        val previousCharacterInfo: CharacterInfo? = null, // 강화 전 캐릭터 정보
        val enhanceExperience: com.ggumtle.domain.rest.model.growth.response.Experience? = null, // 강화 결과 경험치 정보
        val isShowingDailyMission: Boolean = false,

        // 강화 버튼 클릭 여부
        val isEnhanceButtonEnabled: Boolean = false,


        val isNavigating: Boolean = false,

        val isLoading: Boolean = false,
        val errorMessage: String? = null,
        val dialogState: DialogState = DialogState.Hidden,
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data object NavigateToHome : SideEffect
        data object NavigateToAR : SideEffect
    }
}