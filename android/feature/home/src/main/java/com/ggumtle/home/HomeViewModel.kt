package com.ggumtle.home

import androidx.lifecycle.ViewModel
import com.ggumtle.home.model.Friend
import com.ggumtle.home.model.PartyMember
import dagger.hilt.android.lifecycle.HiltViewModel
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class HomeViewModel @Inject constructor(
    // TODO: Repository 주입 예정
) : ViewModel(), ContainerHost<HomeContract.State, HomeContract.SideEffect> {

    override val container: Container<HomeContract.State, HomeContract.SideEffect> = container(HomeContract.State())

    init {
        loadInitialData()
    }

    private fun loadInitialData() = intent {
        // TODO: 사용자 프로필 및 파티 정보 로드
        reduce {
            state.copy(
                userProfile = HomeContract.UserProfile(
                    id = "user1",
                    nickname = "Player1"
                ),
                partyMembers = listOf(
                    PartyMember(
                        id = "user1",
                        nickname = "Player1",
                        isLeader = true,
                        isReady = true
                    )
                ),
                friends = getMockFriends()
            )
        }
        updateGameStartAvailability()
    }

    // 프로필 관련
    fun onProfileClick() = intent {
        reduce { state.copy(isProfileDialogVisible = true) }
    }

    fun onDismissProfileDialog() = intent {
        reduce {
            state.copy(
                isProfileDialogVisible = false,
                isNicknameEditMode = false,
                tempNickname = ""
            )
        }
    }

    fun onEditNicknameClick() = intent {
        reduce {
            state.copy(
                isNicknameEditMode = true,
                tempNickname = state.userProfile.nickname
            )
        }
    }

    fun onNicknameTextChange(nickname: String) = intent {
        reduce { state.copy(tempNickname = nickname) }
    }

    fun onSaveNickname() = intent {
        if (state.tempNickname.isNotBlank()) {
            // TODO: 닉네임 변경 API 호출
            reduce {
                state.copy(
                    userProfile = state.userProfile.copy(nickname = state.tempNickname),
                    isNicknameEditMode = false,
                    tempNickname = ""
                )
            }
            // 파티 멤버 목록에서도 업데이트
            updatePartyMemberNickname(state.userProfile.id, state.tempNickname)
            postSideEffect(HomeContract.SideEffect.ShowToast("닉네임이 변경되었습니다"))
        }
    }

    fun onCancelNicknameEdit() = intent {
        reduce {
            state.copy(
                isNicknameEditMode = false,
                tempNickname = ""
            )
        }
    }

    private fun updatePartyMemberNickname(userId: String, newNickname: String) = intent {
        val updatedMembers = state.partyMembers.map { member ->
            if (member.id == userId) member.copy(nickname = newNickname) else member
        }
        reduce { state.copy(partyMembers = updatedMembers) }
    }

    // 설정 관련
    fun onSettingsClick() = intent {
        reduce { state.copy(isSettingsDialogVisible = true) }
    }

    fun onDismissSettingsDialog() = intent {
        reduce { state.copy(isSettingsDialogVisible = false) }
    }

    fun onLogout() = intent {
        // TODO: 로그아웃 API 호출
        postSideEffect(HomeContract.SideEffect.NavigateToLogin)
    }

    fun onDeleteAccount() = intent {
        // TODO: 회원탈퇴 API 호출
        postSideEffect(HomeContract.SideEffect.ShowToast("회원탈퇴가 처리되었습니다"))
        postSideEffect(HomeContract.SideEffect.NavigateToLogin)
    }

    // 파티 관련
    fun onInviteFriendsClick() = intent {
        reduce { state.copy(isInviteFriendsDialogVisible = true) }
    }

    fun onDismissInviteFriendsDialog() = intent {
        reduce { state.copy(isInviteFriendsDialogVisible = false) }
    }

    fun onInviteFriend(friendId: String) = intent {
        reduce { state.copy(isLoading = true) }
        // TODO: 친구 초대 API 호출
        try {
            val friend = state.friends.find { it.id == friendId }
            friend?.let {
                val newMember = PartyMember(
                    id = it.id,
                    nickname = it.nickname,
                    profileImageUrl = it.profileImageUrl,
                    isReady = false,
                    isLeader = false
                )
                reduce {
                    state.copy(
                        partyMembers = state.partyMembers + newMember,
                        isInviteFriendsDialogVisible = false,
                        isLoading = false
                    )
                }
                updateGameStartAvailability()
                postSideEffect(HomeContract.SideEffect.ShowToast("${it.nickname}님을 초대했습니다"))
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
            postSideEffect(HomeContract.SideEffect.ShowToast("초대에 실패했습니다"))
        }
    }

    fun onToggleReady() = intent {
        if (state.isPartyLeader) return@intent

        // TODO: 레디 상태 변경 API 호출
        val updatedMembers = state.partyMembers.map { member ->
            if (member.id == state.userProfile.id) {
                member.copy(isReady = !member.isReady)
            } else member
        }
        reduce { state.copy(partyMembers = updatedMembers) }
        updateGameStartAvailability()
    }

    fun onStartGame() = intent {
        if (!state.isPartyLeader || !state.canStartGame) return@intent

        // TODO: 게임 시작 API 호출
        postSideEffect(HomeContract.SideEffect.StartGame)
    }

    private fun updateGameStartAvailability() = intent {
        val allReady = state.partyMembers.all { it.isReady }
        reduce { state.copy(canStartGame = allReady && state.partyMembers.isNotEmpty()) }
    }

    // Mock 데이터
    private fun getMockFriends(): List<Friend> = listOf(
        Friend("friend1", "게이머123", isOnline = true),
        Friend("friend2", "프로플레이어", isOnline = true),
        Friend("friend5", "게이머1234", isOnline = true),
        Friend("friend6", "프로플레이어1", isOnline = true),
        Friend("friend3", "친구1", isOnline = false),
        Friend("friend4", "친구2", isOnline = false)
    )
}