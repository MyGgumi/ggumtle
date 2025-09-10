package com.ggumtle.social

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import com.example.designsystem.theme.GameColors
import com.ggumtle.social.component.SocialTabBar
import com.ggumtle.social.component.FriendsTabContent
import com.ggumtle.social.component.AddFriendsTabContent
import com.ggumtle.social.component.UserSearchDialog

@Composable
fun SocialContent(
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
    onOpenProfile: (Long) -> Unit,
    onRemoveFriend: (Long) -> Unit
) {
    var addFriendsSearchQuery by remember { mutableStateOf(TextFieldValue("")) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(GameColors.background)
            .statusBarsPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(20.dp)
        ) {
            SocialTabBar(
                currentTab = state.currentTab,
                onTabSelected = onTabSelected
            )

            when (state.currentTab) {
                SocialContract.SocialTab.FRIENDS -> FriendsTabContent(
                    friends = state.friends,
                    searchQuery = state.friendsSearchQuery,
                    isLoading = state.isLoading,
                    onUpdateSearchQuery = { query ->
                        onUpdateFriendsSearchQuery(query)
                    },
                    onOpenProfile = onOpenProfile,
                    onRemoveFriend = onRemoveFriend,
                )
                SocialContract.SocialTab.ADD_FRIENDS -> AddFriendsTabContent(
                    receivedRequests = state.receivedRequests,
                    sentRequests = state.sentRequests,
                    isLoading = state.isLoading,
                    onAcceptFriendRequest = onAcceptFriendRequest,
                    onRejectFriendRequest = onRejectFriendRequest,
                    onCancelSentRequest = onCancelSentRequest,
                    onOpenProfile = onOpenProfile,
                    onShowSearchDialog = onShowSearchDialog
                )
            }
        }

        // 검색 다이얼로그
        if (state.isSearchDialogVisible) {
            UserSearchDialog(
                searchQuery = state.addFriendsSearchQuery,
                searchResults = state.searchResults,
                isLoading = state.isSearchLoading,
                onUpdateSearchQuery = { query ->
                    onUpdateAddFriendsSearchQuery(query)
                },
                onSendFriendRequest = onSendFriendRequest,
                onOpenProfile = onOpenProfile,
                onDismiss = {
                    onDismissSearchDialog()
                }
            )
        }
    }
}