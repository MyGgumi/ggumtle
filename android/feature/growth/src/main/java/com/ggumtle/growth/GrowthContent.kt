package com.ggumtle.growth

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
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
    onCharacterSwipe: (Int) -> Unit = {},
    onEnhanceClick: () -> Unit = {},
    onHideEnhanceSuccessDialog: () -> Unit = {},
    onHideDailyMissionDialog: () -> Unit = {},
    onClaimMissionReward: (String) -> Unit = {},
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
                characterName = state.characters.getOrNull(state.selectedCharacterIndex)?.name ?: "힐러 몽깅이",
                selectedIndex = state.selectedCharacterIndex,
                totalCharacters = state.characters.size,
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
                characterInfo = state.characters.getOrNull(state.selectedCharacterIndex),
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

        // 강화 성공 다이얼로그
        if (state.isShowingEnhanceSuccess && state.previousCharacterInfo != null) {
            val previousChar = state.previousCharacterInfo
            val currentChar = state.characters.getOrNull(state.selectedCharacterIndex)

            if (currentChar != null) {
                EnhanceSuccessDialog(
                    isVisible = state.isShowingEnhanceSuccess,
                    nextSuccessRate = currentChar.successRate,
                    currentLevel = previousChar.level,
                    nextLevel = currentChar.level,
                    currentStat = previousChar.currentStat,
                    nextStat = currentChar.currentStat,
                    statisticName = currentChar.statisticName,
                    onDismiss = onHideEnhanceSuccessDialog,
                    modifier = Modifier.fillMaxSize()
                )
            }
        }

        // 일일 미션 다이얼로그
        DailyMissionDialog(
            isVisible = state.isShowingDailyMission,
            missions = state.dailyMission.missions,
            completedCount = state.dailyMission.completedMissions,
            totalCount = state.dailyMission.totalMissions,
            onDismiss = onHideDailyMissionDialog,
            onClaimReward = { mission -> onClaimMissionReward(mission.id) }
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

