package com.ggumtle.growth

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.hilt.navigation.compose.hiltViewModel
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import com.ggumtle.designsystem.dialog.DialogContainer

@Composable
fun GrowthRoute(
    onNavigateToHome: () -> Unit = {},
    viewModel: GrowthViewModel = hiltViewModel()
) {
    val state by viewModel.collectAsState()

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is GrowthContract.SideEffect.ShowToast -> {
                // TODO: Toast 표시
            }
            is GrowthContract.SideEffect.NavigateToHome -> onNavigateToHome()
        }
    }

    GrowthScreen(
        state = state,
        onBackClick = viewModel::onBackClick
    )

    DialogContainer(
        dialogState = state.dialogState,
        onDismiss = { viewModel.hideDialog() }
    )
}