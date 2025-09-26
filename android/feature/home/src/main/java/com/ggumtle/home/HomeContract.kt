package com.ggumtle.home

import com.ggumtle.domain.websocket.model.Friend
import com.ggumtle.domain.websocket.model.PartyMember
import com.ggumtle.home.model.PartyInfo
import com.ggumtle.home.model.UserProfile
import com.ggumtle.home.model.InviteRequest
import com.ggumtle.designsystem.dialog.DialogState
import com.ggumtle.domain.rest.model.growth.response.Mongging

object HomeContract {

    data class State(
        // 사용자 정보
        val userProfile: UserProfile = UserProfile(-1, "UNKNOWN"),

        // 파티 멤버 정보
        val partyMembers: List<PartyMember> = emptyList(),
        // 파티 정보
        val partyInfo: PartyInfo? = null,
        // 파티 리더 여부
        val isPartyLeader: Boolean = true,
        // 게임 시작 가능 여부
        val canStartGame: Boolean = false,
        // 준비 여부
        val isReady: Boolean = false,
        // 게임 찾는 중 여부
        val isSearchingGame: Boolean = false,
        // 매칭 경과 시간 (초)
        val matchmakingTimeSeconds: Int = 0,

        // UI 상태
        val isProfileDialogVisible: Boolean = false,
        val isSettingsDialogVisible: Boolean = false,
        val isInviteFriendsDialogVisible: Boolean = false,
        val isInviteRequestsDialogVisible: Boolean = false,
        val isMenuExpanded: Boolean = false,
        val isNicknameEditMode: Boolean = false,
        val tempNickname: String = "",
        val dialogState: DialogState = DialogState.Hidden,

        // 친구 목록 (초대용)
        val friends: List<Friend> = emptyList(),
        // 파티 초대 목록
        val inviteRequests: List<InviteRequest> = emptyList(),

        val isNavigating: Boolean = false,
        val coin: Int = 0,

        // 내 몽깅이 정보
        val monggings: List<Mongging> = emptyList(),
        // 현재 선택된 인덱스
        val selectedCharacterIndex: Int = 0,

        val isLoading: Boolean = false,
        val errorMessage: String? = null
    )

    sealed interface SideEffect {
        data class ShowToast(val message: String) : SideEffect
        data object NavigateToSocial : SideEffect
        data object NavigateToGrowth : SideEffect
        data object NavigateToInGame : SideEffect
        data object StartGame : SideEffect
    }
}