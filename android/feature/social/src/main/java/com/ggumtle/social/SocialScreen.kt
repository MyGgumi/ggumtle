package com.ggumtle.social

import androidx.compose.runtime.Composable
import androidx.compose.ui.text.input.TextFieldValue

@Composable
fun SocialScreen(
    state: SocialContract.State,
    onTabSelected: (SocialContract.SocialTab) -> Unit,
    onUpdateFriendsSearchQuery: (TextFieldValue) -> Unit,
    onUpdateAddFriendsSearchQuery: (TextFieldValue) -> Unit,
    onShowSearchDialog: () -> Unit,
    onDismissSearchDialog: () -> Unit,
    onAcceptFriendRequest: (Long) -> Unit,
    onRejectFriendRequest: (Long) -> Unit,
    onCancelSentRequest: (Long) -> Unit,
    onSendFriendRequest: (Long) -> Unit,
    onSendFriendRequestAndCloseDialog: (Long) -> Unit,
    onOpenProfile: (Long) -> Unit,
    onRemoveFriend: (Long) -> Unit,
    onClickBack: () -> Unit
) {
    SocialContent(
        state = state,
        onTabSelected = onTabSelected,
        onUpdateFriendsSearchQuery = onUpdateFriendsSearchQuery,
        onUpdateAddFriendsSearchQuery = onUpdateAddFriendsSearchQuery,
        onShowSearchDialog = onShowSearchDialog,
        onDismissSearchDialog = onDismissSearchDialog,
        onAcceptFriendRequest = onAcceptFriendRequest,
        onRejectFriendRequest = onRejectFriendRequest,
        onCancelSentRequest = onCancelSentRequest,
        onSendFriendRequest = onSendFriendRequest,
        onOpenProfile = onOpenProfile,
        onSendFriendRequestAndCloseDialog = onSendFriendRequestAndCloseDialog,
        onRemoveFriend = onRemoveFriend,
        onClickBack = onClickBack
    )
}