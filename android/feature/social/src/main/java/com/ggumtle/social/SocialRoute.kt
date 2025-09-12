package com.ggumtle.social

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.navigation.NavController
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect

@Composable
fun SocialRoute(
    onNavigateToHome: () -> Unit = {},
    viewModel: SocialViewModel = hiltViewModel()
) {
    val state by viewModel.collectAsState()

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is SocialContract.SideEffect.ShowToast -> {
                // TODO: Toast 표시
            }
            is SocialContract.SideEffect.NavigateToProfile -> {
                // TODO: 프로필 화면으로 네비게이션
            }

            is SocialContract.SideEffect.NavigateToHome -> onNavigateToHome()
        }
    }

    SocialScreen(
        state = state,
        onTabSelected = viewModel::onTabSelected,
        onUpdateFriendsSearchQuery = viewModel::onUpdateFriendsSearchQuery,
        onUpdateAddFriendsSearchQuery = viewModel::onUpdateAddFriendsSearchQuery,
        onShowSearchDialog = viewModel::onShowSearchDialog,
        onDismissSearchDialog = viewModel::onDismissSearchDialog,
        onAcceptFriendRequest = viewModel::onAcceptFriendRequest,
        onRejectFriendRequest = viewModel::onRejectFriendRequest,
        onCancelSentRequest = viewModel::onCancelSentRequest,
        onSendFriendRequest = viewModel::onSendFriendRequest,
        onOpenProfile = viewModel::onOpenProfile,
        onRemoveFriend = viewModel::onRemoveFriend,
        onClickBack = viewModel::onClickBack
    )
}