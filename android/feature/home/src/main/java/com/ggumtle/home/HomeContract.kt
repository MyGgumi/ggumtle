package com.ggumtle.home

import com.ggumtle.home.model.PartyMember
import com.ggumtle.home.model.Friend

object HomeContract {

    data class State(
        // 사용자 정보
        val userProfile: UserProfile = UserProfile(),

        // 파티 정보
        val partyMembers: List<PartyMember> = emptyList(),
        val isPartyLeader: Boolean = true,
        val canStartGame: Boolean = false, // 모든 멤버가 레디된 상태인지

        // UI 상태
        val isProfileDialogVisible: Boolean = false,
        val isSettingsDialogVisible: Boolean = false,
        val isInviteFriendsDialogVisible: Boolean = false,
        val isNicknameEditMode: Boolean = false,
        val tempNickname: String = "",

        // 친구 목록 (초대용)
        val friends: List<Friend> = emptyList(),
        val isLoading: Boolean = false,
        val errorMessage: String? = null
    )

    data class UserProfile(
        val id: String = "",
        val nickname: String = "Player1",
        val profileImageUrl: String? = null
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data object NavigateToLogin : SideEffect
        data object StartGame : SideEffect
    }
}