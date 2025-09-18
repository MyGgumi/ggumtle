package com.ggumtle.growth

import android.widget.Toast
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.platform.LocalContext
import androidx.hilt.navigation.compose.hiltViewModel
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import com.ggumtle.designsystem.dialog.DialogContainer

@Composable
fun GrowthRoute(
    onNavigateToHome: () -> Unit = {},
    onNavigateToMission: () -> Unit = {},
    viewModel: GrowthViewModel = hiltViewModel()
) {
    val state by viewModel.collectAsState()
    val context = LocalContext.current

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is GrowthContract.SideEffect.ShowToast -> {
                Toast.makeText(context, sideEffect.message, Toast.LENGTH_SHORT).show()
            }
            is GrowthContract.SideEffect.NavigateToHome -> onNavigateToHome()
            is GrowthContract.SideEffect.NavigateToAR -> onNavigateToMission()

        }
    }

    if (!state.isNavigating) {
        GrowthContent(
            state = state,
            onBackClick = viewModel::onBackClick,
            onARClick = viewModel::onARClick,
            onDailyMissionClick = viewModel::showDailyMissionDialog,
            onCharacterSwipe = viewModel::onCharacterSwipe,
            onEnhanceClick = viewModel::onEnhanceClick,
            onHideEnhanceSuccessDialog = viewModel::hideEnhanceSuccessDialog,
            onHideDailyMissionDialog = viewModel::hideDailyMissionDialog,
            onClaimMissionReward = viewModel::claimMissionReward
        )

        DialogContainer(
            dialogState = state.dialogState,
            onDismiss = { viewModel.hideDialog() }
        )
    }
}