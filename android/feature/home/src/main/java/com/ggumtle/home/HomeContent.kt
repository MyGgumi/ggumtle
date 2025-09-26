package com.ggumtle.home

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.ui.draw.clipToBounds
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.ggumtle.home.component.*
import com.ggumtle.home.model.UserProfile
import com.ggumtle.domain.websocket.model.PartyMember
import com.ggumtle.core.designsystem.R

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
    Box(
        modifier = Modifier.fillMaxSize()
    ) {
        Column {
            // 상단 UI 영역
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .background(Color.Black.copy(alpha = 0.1f))
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.Top
            ) {
                // 왼쪽 상단 - 프로필 섹션
                ProfileSection(
                    userProfile = state.userProfile,
                    onClick = onProfileClick,
                    coin = state.coin
                )

                // 빈 공간
                Spacer(modifier = Modifier.size(48.dp))
            }

            GrowthButton(
                onClick = onGrowthClick
            )

            FriendButton(
                onClick = onSocialClick
            )
        }

        // 오른쪽 상단 - 메뉴 탭 (독립적 배치)
        MenuTab(
            isExpanded = state.isMenuExpanded,
            isInParty = state.partyMembers.size >= 2,
            onTabClick = onMenuTabClick,
            onSettingsClick = onSettingsClick,
            onInviteListClick = onInviteListClick,
            onLeavePartyClick = onLeaveParty,
            onSocialClick = onSocialClick,
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(end = 16.dp, top = 8.dp)
        )

        // 하단 중앙 - 파티 섹션
        PartySection(
            partyMembers = state.partyMembers,
            isPartyLeader = state.isPartyLeader,
            canStartGame = state.canStartGame,
            isGameStartLoading = state.isGameStartLoading,
            onInviteFriendsClick = onInviteFriendsClick,
            onToggleReady = onToggleReady,
            onStartGame = onStartGame,
            onCancelGameSearch = onCancelGameSearch,
            isReady = state.isReady,
            isSearchingGame = state.isSearchingGame,
            matchmakingTimeSeconds = state.matchmakingTimeSeconds,
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

        // 초대 요청 목록 다이얼로그
        if (state.isInviteRequestsDialogVisible) {
            InviteRequestsDialog(
                inviteRequests = state.inviteRequests,
                onDismiss = onDismissInviteRequestsDialog,
                onAcceptInvite = onAcceptInvite,
                onDeclineInvite = onDeclineInvite
            )
        }

        // 게임 시작 다이얼로그
        if (state.isGameStartingDialogVisible) {
            GameStartingDialog()
        }
    }
}