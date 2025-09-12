package com.ggumtle.social

import androidx.compose.ui.text.input.TextFieldValue
import com.example.domain.websocket.model.Friend
import com.example.domain.websocket.model.FriendRequest
import com.ggumtle.social.model.SentRequest
import com.ggumtle.social.model.User

object SocialContract {

    data class State(
        val currentTab: SocialTab = SocialTab.FRIENDS,
        val friends: List<Friend> = emptyList(),
        val receivedRequests: List<FriendRequest> = emptyList(),
        val sentRequests: List<SentRequest> = emptyList(),
        val searchResults: List<User> = emptyList(),
        val friendsSearchQuery: TextFieldValue = TextFieldValue(""),
        val addFriendsSearchQuery: TextFieldValue = TextFieldValue(""),
        val isLoading: Boolean = false,
        val isSearchLoading: Boolean = false,
        val isSearchDialogVisible: Boolean = false,
        val errorMessage: String? = null
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data class NavigateToProfile(val userId: String) : SideEffect
    }

    enum class SocialTab {
        FRIENDS, ADD_FRIENDS
    }
}