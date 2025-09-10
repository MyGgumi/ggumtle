package com.ggumtle.home

import androidx.compose.runtime.Composable

@Composable
fun HomeScreen(
    state: HomeContract.State,
    onProfileClick: () -> Unit,
    onDismissProfileDialog: () -> Unit,
    onEditNicknameClick: () -> Unit,
    onNicknameTextChange: (String) -> Unit,
    onSaveNickname: () -> Unit,
    onCancelNicknameEdit: () -> Unit,
    onSettingsClick: () -> Unit,
    onDismissSettingsDialog: () -> Unit,
    onLogout: () -> Unit,
    onDeleteAccount: () -> Unit,
    onInviteFriendsClick: () -> Unit,
    onDismissInviteFriendsDialog: () -> Unit,
    onInviteFriend: (String) -> Unit,
    onToggleReady: () -> Unit,
    onStartGame: () -> Unit
) {
    HomeContent(
        state = state,
        onProfileClick = onProfileClick,
        onDismissProfileDialog = onDismissProfileDialog,
        onEditNicknameClick = onEditNicknameClick,
        onNicknameTextChange = onNicknameTextChange,
        onSaveNickname = onSaveNickname,
        onCancelNicknameEdit = onCancelNicknameEdit,
        onSettingsClick = onSettingsClick,
        onDismissSettingsDialog = onDismissSettingsDialog,
        onLogout = onLogout,
        onDeleteAccount = onDeleteAccount,
        onInviteFriendsClick = onInviteFriendsClick,
        onDismissInviteFriendsDialog = onDismissInviteFriendsDialog,
        onInviteFriend = onInviteFriend,
        onToggleReady = onToggleReady,
        onStartGame = onStartGame
    )
}