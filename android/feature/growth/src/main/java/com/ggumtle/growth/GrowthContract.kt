package com.ggumtle.growth

import com.ggumtle.designsystem.dialog.DialogState
import com.ggumtle.growth.model.CharacterInfo
import com.ggumtle.growth.model.DailyMission
import com.ggumtle.growth.model.Mission

object GrowthContract {

    data class State(
        val isLoading: Boolean = false,
        val errorMessage: String? = null,
        val dialogState: DialogState = DialogState.Hidden,
        val dreamCoin: Int = 9999,
        val selectedCharacterIndex: Int = 0,
        val characters: List<CharacterInfo> = listOf(
            CharacterInfo(
                name = "힐러 몽깅이",
                level = 4,
                currentStat = 1.2f,
                nextLevelStat = 1.25f,
                enhancementCost = 4000,
                successRate = 35,
                progressRatio = 0.3f,
                statisticName = "치료속도"
            )
        ),
        val dailyMission: DailyMission = DailyMission(
            totalMissions = 5,
            completedMissions = 2,
            missions = listOf(
                Mission(
                    id = "1",
                    title = "몽깅이랑 사진찍기",
                    reward = 100,
                    isCompleted = true,
                    isRewarded = false
                ),
                Mission(
                    id = "2",
                    title = "악몽 1회 탈출",
                    reward = 100,
                    isCompleted = true,
                    isRewarded = true
                ),
                Mission(
                    id = "3",
                    title = "친구에게 '좋은 꿈' 보내기",
                    reward = 50,
                    isCompleted = false,
                    isRewarded = false
                ),
                Mission(
                    id = "4",
                    title = "몽깅이 쓰다듬기",
                    reward = 50,
                    isCompleted = true,
                    isRewarded = true
                ),
                Mission(
                    id = "5",
                    title = "미션 완료하기",
                    reward = 75,
                    isCompleted = false,
                    isRewarded = false
                )
            )
        ),
        val isEnhanceButtonEnabled: Boolean = false,
        val isShowingEnhanceFailure: Boolean = false,
        val isShowingEnhanceSuccess: Boolean = false,
        val previousCharacterInfo: CharacterInfo? = null, // 강화 전 캐릭터 정보
        val isShowingDailyMission: Boolean = false
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data object NavigateToHome : SideEffect
        data object NavigateToAR : SideEffect
    }
}