package com.ggumtle.home

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.window.Dialog
import androidx.hilt.navigation.compose.hiltViewModel
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import com.ggumtle.designsystem.dialog.DialogContainer

@Composable
fun HomeRoute(
    onNavigateToSocial: () -> Unit = {},
    onNavigateToGrowth: () -> Unit = {},
    viewModel: HomeViewModel = hiltViewModel()
) {
    val state by viewModel.collectAsState()

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is HomeContract.SideEffect.ShowToast -> {
                // TODO: Toast 표시
            }
            is HomeContract.SideEffect.StartGame -> {
                // TODO: 게임 시작 로직
            }
            is HomeContract.SideEffect.NavigateToSocial -> onNavigateToSocial()
            is HomeContract.SideEffect.NavigateToGrowth -> onNavigateToGrowth()
        }
    }

    HomeScreen(
        state = state,
        onProfileClick = viewModel::onProfileClick,
        onDismissProfileDialog = viewModel::onDismissProfileDialog,
        onEditNicknameClick = viewModel::onEditNicknameClick,
        onNicknameTextChange = viewModel::onNicknameTextChange,
        onSaveNickname = viewModel::onChangeNickname,
        onCancelNicknameEdit = viewModel::onCancelNicknameEdit,
        onSettingsClick = viewModel::onSettingsClick,
        onDismissSettingsDialog = viewModel::onDismissSettingsDialog,
        onLogout = viewModel::onLogout,
        onDeleteAccount = viewModel::onDeleteAccount,
        onInviteFriendsClick = viewModel::onInviteFriendsClick,
        onDismissInviteFriendsDialog = viewModel::onDismissInviteFriendsDialog,
        onInviteFriend = viewModel::onInviteFriend,
        onToggleReady = viewModel::onToggleReady,
        onStartGame = viewModel::onStartGame,
        onCancelGameSearch = viewModel::onCancelGameSearch,
        onMenuTabClick = viewModel::onMenuTabClick,
        onInviteListClick = viewModel::onInviteListClick,
        onLeaveParty = viewModel::onShowLeavePartyDialog,
        onDismissInviteRequestsDialog = viewModel::onDismissInviteRequestsDialog,
        onAcceptInvite = viewModel::onAcceptInvite,
        onDeclineInvite = viewModel::onDeclineInvite,
        onSocialClick = viewModel::onSocialClick,
        onGrowthClick = viewModel::onGrowthClick
    )

    DialogContainer(
        dialogState = state.dialogState,
        onDismiss = { viewModel.hideDialog() }
    )
}