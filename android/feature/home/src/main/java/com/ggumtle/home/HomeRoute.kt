package com.ggumtle.home

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.hilt.navigation.compose.hiltViewModel
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import com.example.designsystem.dialog.DialogContainer

@Composable
fun HomeRoute(
    onNavigateToAuth: () -> Unit = {},
    viewModel: HomeViewModel = hiltViewModel()
) {
    val state by viewModel.collectAsState()

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is HomeContract.SideEffect.ShowToast -> {
                // TODO: Toast 표시
            }
            is HomeContract.SideEffect.NavigateToLogin -> {
                onNavigateToAuth
            }
            is HomeContract.SideEffect.StartGame -> {
                // TODO: 게임 시작 로직
            }
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
        onDeclineInvite = viewModel::onDeclineInvite
    )

    DialogContainer(
        dialogState = state.dialogState,
        onDismiss = { viewModel.hideDialog() }
    )
}