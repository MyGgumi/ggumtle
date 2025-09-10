package com.ggumtle.home

import androidx.compose.foundation.layout.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.ggumtle.home.component.*

@Composable
fun HomeContent(
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
    Box(
        modifier = Modifier.fillMaxSize()
    ) {
        // 상단 UI 영역
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.Top
        ) {
            // 왼쪽 상단 - 프로필
            ProfileSection(
                userProfile = state.userProfile,
                onClick = onProfileClick
            )

            // 오른쪽 상단 - 설정 버튼
            SettingsButton(
                onClick = onSettingsClick
            )
        }

        // 하단 중앙 - 파티 섹션
        PartySection(
            partyMembers = state.partyMembers,
            isPartyLeader = state.isPartyLeader,
            canStartGame = state.canStartGame,
            isLoading = state.isLoading,
            onInviteFriendsClick = onInviteFriendsClick,
            onToggleReady = onToggleReady,
            onStartGame = onStartGame,
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .padding(16.dp)
        )

        // 프로필 다이얼로그
        if (state.isProfileDialogVisible) {
            ProfileDialog(
                userProfile = state.userProfile,
                isNicknameEditMode = state.isNicknameEditMode,
                tempNickname = state.tempNickname,
                onDismiss = onDismissProfileDialog,
                onEditNicknameClick = onEditNicknameClick,
                onNicknameTextChange = onNicknameTextChange,
                onSaveNickname = onSaveNickname,
                onCancelNicknameEdit = onCancelNicknameEdit
            )
        }

        // 설정 다이얼로그
        if (state.isSettingsDialogVisible) {
            SettingsDialog(
                onDismiss = onDismissSettingsDialog,
                onLogout = onLogout,
                onDeleteAccount = onDeleteAccount
            )
        }

        // 친구 초대 다이얼로그
        if (state.isInviteFriendsDialogVisible) {
            InviteFriendsDialog(
                friends = state.friends,
                partyMemberIds = state.partyMembers.map { it.id },
                isLoading = state.isLoading,
                onDismiss = onDismissInviteFriendsDialog,
                onInviteFriend = onInviteFriend
            )
        }
    }
}