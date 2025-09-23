package com.ggumtle.growth

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.ggumtle.growth.component.*

@Composable
fun GrowthContent(
    state: GrowthContract.State,
    onBackClick: () -> Unit = {},
    onARClick: () -> Unit = {},
    onDailyMissionClick: () -> Unit = {},
    onCharacterSwipe: (Int, Boolean) -> Unit = { index, isNext -> },
    onEnhanceClick: () -> Unit = {},
    onHideEnhanceSuccessDialog: () -> Unit = {},
    onHideDailyMissionDialog: () -> Unit = {},
    onClaimMissionReward: (Long) -> Unit = {},
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .fillMaxSize()
            .background(Color.Transparent)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = 16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            GrowthTopBar(
                dreamCoin = state.dreamCoin,
                onBackClick = onBackClick,
                onARClick = onARClick,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(modifier = Modifier.height(24.dp))

            CharacterDisplay(
                characterName = state.myCharacters.getOrNull(state.selectedCharacterIndex)?.monggingClass?.displayName
                    ?: "- 몽깅이",
                selectedIndex = state.selectedCharacterIndex,
                totalCharacters = state.myCharacters.size,
                onSwipe = onCharacterSwipe,
                modifier = Modifier
                    .fillMaxWidth()
                    .weight(0.4f)
            )

            Spacer(modifier = Modifier.height(16.dp))

            DailyMissionBanner(
                dailyMission = state.dailyMission,
                onClick = onDailyMissionClick,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(modifier = Modifier.height(16.dp))

            EnhancementSection(
                characterInfo = state.myCharacters.getOrNull(state.selectedCharacterIndex),
                currentCoin = state.dreamCoin,
                isEnhanceEnabled = state.isEnhanceButtonEnabled,
                onEnhanceClick = onEnhanceClick,
                modifier = Modifier.fillMaxWidth()
            )

            Spacer(modifier = Modifier.height(16.dp))
        }

        // 강화 실패 오버레이
        EnhanceFailureOverlay(
            isVisible = state.isShowingEnhanceFailure,
            modifier = Modifier.fillMaxSize()
        )

        // 다이얼로그 오버레이 배경
        if (state.isShowingEnhanceSuccess || state.isShowingDailyMission) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(Color.Black.copy(alpha = 0.8f))
                    .clickable(
                        interactionSource = remember { androidx.compose.foundation.interaction.MutableInteractionSource() },
                        indication = null
                    ) {
                        // 배경 클릭 시 다이얼로그 닫기
                        if (state.isShowingEnhanceSuccess) onHideEnhanceSuccessDialog()
                        if (state.isShowingDailyMission) onHideDailyMissionDialog()
                    }
            )
        }

        // 강화 성공 다이얼로그
        if (state.isShowingEnhanceSuccess && state.enhanceExperience != null) {
            EnhanceSuccessDialog(
                isVisible = state.isShowingEnhanceSuccess,
                experience = state.enhanceExperience,
                onDismiss = onHideEnhanceSuccessDialog,
                modifier = Modifier.fillMaxSize()
            )
        }

        // 일일 미션 다이얼로그
        DailyMissionDialog(
            isVisible = state.isShowingDailyMission,
            missions = state.dailyMission.missions,
            totalCount = state.dailyMission.totalMissionsCount,
            completedCount = state.dailyMission.completedMissionsCount,
            remainingCount = state.dailyMission.remainingMissionsCount,
            afterRewardCount = state.dailyMission.afterRewordMissionsCount,
            onClaimReward = { mission -> onClaimMissionReward(mission.memberMissionId) },
            onDismiss = onHideDailyMissionDialog,
        )
    }
}

@Preview(showBackground = true)
@Composable
fun GrowthContentPreview() {
    GrowthContent(
        state = GrowthContract.State()
    )
}

