package com.ggumtle.social

import androidx.compose.ui.text.input.TextFieldValue
import com.ggumtle.domain.websocket.model.Friend
import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.social.model.SentRequest
import com.ggumtle.social.model.User

object SocialContract {

    data class State(
        // 현재 탭
        val currentTab: SocialTab = SocialTab.FRIENDS,

        // 친구 목록
        val friends: List<Friend> = emptyList(),

        // 받은 요청 목록
        val receivedRequests: List<FriendRequest> = emptyList(),

        // 보낸 요청 목록
        val sentRequests: List<SentRequest> = emptyList(),

        // 찾은 유저 목록
        val searchResults: List<User> = emptyList(),

        // 친구 목록 내 검색 쿼리
        val friendsSearchQuery: TextFieldValue = TextFieldValue(""),

        // 사용자 검색 쿼리
        val addFriendsSearchQuery: TextFieldValue = TextFieldValue(""),

        // 사용자 검색 ui
        val isSearchLoading: Boolean = false,
        val isSearchDialogVisible: Boolean = false,

        val errorMessage: String? = null,
        val isLoading: Boolean = false,
        )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data class NavigateToProfile(val userId: String) : SideEffect
        data object NavigateToHome : SideEffect
    }

    enum class SocialTab {
        FRIENDS, ADD_FRIENDS
    }
}