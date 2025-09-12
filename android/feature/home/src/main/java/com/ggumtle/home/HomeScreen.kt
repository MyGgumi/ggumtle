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
    onInviteFriend: (Long) -> Unit,
    onToggleReady: () -> Unit,
    onStartGame: () -> Unit,
    onCancelGameSearch: () -> Unit,
    onMenuTabClick: () -> Unit,
    onInviteListClick: () -> Unit,
    onLeaveParty: () -> Unit,
    onDismissInviteRequestsDialog: () -> Unit,
    onAcceptInvite: (String) -> Unit,
    onDeclineInvite: (String) -> Unit,
    onSocialClick: () -> Unit,
    onGrowthClick: () -> Unit
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
        onStartGame = onStartGame,
        onCancelGameSearch = onCancelGameSearch,
        onMenuTabClick = onMenuTabClick,
        onInviteListClick = onInviteListClick,
        onLeaveParty = onLeaveParty,
        onDismissInviteRequestsDialog = onDismissInviteRequestsDialog,
        onAcceptInvite = onAcceptInvite,
        onDeclineInvite = onDeclineInvite,
        onSocialClick = onSocialClick,
        onGrowthClick = onGrowthClick
    )
}