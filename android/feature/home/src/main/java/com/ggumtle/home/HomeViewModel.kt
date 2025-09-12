package com.ggumtle.home

import androidx.lifecycle.ViewModel
import com.example.datastore.AuthManager
import com.example.domain.model.MemberConnectionState
import com.example.domain.websocket.usecase.home.AcceptPartyInvitationUseCase
import com.example.domain.websocket.usecase.home.CreatePartyUseCase
import com.example.domain.websocket.usecase.home.InvitePartyUseCase
import com.example.domain.websocket.usecase.home.LeavePartyUseCase
import com.example.domain.websocket.usecase.home.ObserveAcceptPartyInvitationUseCase
import com.example.domain.websocket.usecase.home.ObserveCreatePartyUseCase
import com.example.domain.websocket.usecase.home.ObserveInvitePartyUseCase
import com.example.domain.websocket.usecase.home.ObserveLeavePartyUseCase
import com.example.domain.websocket.usecase.social.GetFriendsUseCase
import com.example.domain.websocket.usecase.social.ObserveGetFriendsUseCase
import com.ggumtle.home.model.PartyInfo
import com.ggumtle.home.model.PartyMember
import com.ggumtle.home.model.UserProfile
import com.example.designsystem.dialog.DialogState
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.model.UnityMethod
import com.example.domain.unity.model.UnityTarget
import com.example.domain.websocket.usecase.home.ObserveReadyGameUseCase
import com.example.domain.websocket.usecase.home.ReadyGameUseCase
import com.example.domain.websocket.usecase.home.UnReadyGameUseCase
import dagger.hilt.android.lifecycle.HiltViewModel
import android.util.Log
import androidx.lifecycle.viewModelScope
import com.example.datastore.LogoutReason
import com.example.domain.websocket.model.DreamStatus
import com.example.domain.websocket.usecase.home.ObserveStartGameUseCase
import com.example.domain.websocket.usecase.home.StartGameUseCase
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.launch
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val unitySendManager: UnitySendManager,
    private val authManager: AuthManager,
    private val createPartyUseCase: CreatePartyUseCase,
    private val observeCreatePartyUseCase: ObserveCreatePartyUseCase,
    private val getFriendsUseCase: GetFriendsUseCase,
    private val observeGetFriendsUseCase: ObserveGetFriendsUseCase,
    private val invitePartyUseCase: InvitePartyUseCase,
    private val observeInvitePartyUseCase: ObserveInvitePartyUseCase,
    private val acceptPartyInvitationUseCase: AcceptPartyInvitationUseCase,
    private val observeAcceptPartyInvitationUseCase: ObserveAcceptPartyInvitationUseCase,
    private val leavePartyUseCase: LeavePartyUseCase,
    private val observeLeavePartyUseCase: ObserveLeavePartyUseCase,
    private val readyGameUseCase: ReadyGameUseCase,
    private val observeReadyGameUseCase: ObserveReadyGameUseCase,
    private val unReadyGameUseCase: UnReadyGameUseCase,
    private val startGameUseCase: StartGameUseCase,
    private val observeStartGameUseCase: ObserveStartGameUseCase
) : ViewModel(), ContainerHost<HomeContract.State, HomeContract.SideEffect> {

    override val container: Container<HomeContract.State, HomeContract.SideEffect> =
        container(HomeContract.State())
        
    // 매칭 타이머 Job
    private var matchmakingTimerJob: Job? = null

    init {
        createParty()
        loadProfile()
        observeHomeEvent()
    }

    private fun observeHomeEvent() {
        val myId = authManager.getMemberId()

        observeAcceptPartyInvitationEvents(myId)
        observeLeavePartyEvents(myId)
        observeReadyEvent(myId)
        observeGameStart()
    }

    // 초대 수락 이벤트 확인
    fun observeAcceptPartyInvitationEvents(myId: Long?) = intent {
        observeAcceptPartyInvitationUseCase.invoke()
            .collect { result ->
                if (result.joinedMemberId == myId) {
                    // TODO: 파티 현황 API 호출 후 업데이트
                    // TODO: Unity 통신 -> 파티 현황에 맞게 플레이어 업데이트
                } else {
                    // TODO: Unity 통신 -> 입장한 플레이어 추가
                    val newPartyMember = PartyMember(id = result.joinedMemberId, nickname = result.joinedMemberNickname)
                    reduce { state.copy(partyMembers = state.partyMembers + newPartyMember) }
                }
            }
    }

    fun observeLeavePartyEvents(myId: Long?) = intent {
        observeLeavePartyUseCase.invoke()
            .collect { result ->
                if (result.leftMemberId == myId) return@collect

                val updatedMembers = state.partyMembers
                    .filterNot { it.id == result.leftMemberId }
                    .map { member ->
                        when {
                            result.newLeaderId == member.id -> member.copy(isLeader = true)
                            else -> member
                        }
                    }
                // TODO: Unity 통신 -> 나간 플레이어 삭제
                reduce {
                    state.copy(
                        partyMembers = updatedMembers,
                        isPartyLeader = myId == result.newLeaderId
                    )
                }
            }
    }

    // 파티 생성
    fun createParty() = intent {
        reduce { state.copy(isLoading = true) }
        try {
            createPartyUseCase.invoke()
            val result = observeCreatePartyUseCase.invoke().first()
            val newPartyMember = PartyMember(
                id = state.userProfile.id,
                nickname = state.userProfile.nickname,
                isLeader = true
            )
            // TODO: Unity 통신 -> 혼자 있는 상태로 플레이어 업데이트
            reduce {
                state.copy(
                    partyInfo = PartyInfo(partyId = result.partyId),
                    partyMembers = listOf(newPartyMember),
                    isPartyLeader = true,
                    canStartGame = true,
                    isReady = false,
                    isLoading = false,
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 파티 초대
    fun onInviteFriend(friendId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            invitePartyUseCase.invoke(friendId)
            val result = observeInvitePartyUseCase.invoke().first()
            reduce {
                state.copy(
                    isInviteFriendsDialogVisible = false,
                    isLoading = false
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 초대 가능 친구 목록 확인 다이얼 로그 열기
    fun onInviteFriendsClick() = intent {
        reduce { state.copy(isLoading = true) }
        try {
            getFriendsUseCase.invoke()
            val result = observeGetFriendsUseCase.invoke().first()
            reduce {
                state.copy(
                    friends = result.friends.filter { it.connectionState == MemberConnectionState.ONLINE },
                    isLoading = false,
                    isInviteFriendsDialogVisible = true
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 초대 가능 친구 목록 확인 다이얼 로그 닫기
    fun onDismissInviteFriendsDialog() =
        intent { reduce { state.copy(isInviteFriendsDialogVisible = false) } }

    // 파티 초대 수락
    fun onAcceptInvite(requestId: String) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            onLeaveParty(isCreateParty = false)
            acceptPartyInvitationUseCase.invoke(requestId)
            reduce {
                state.copy(
                    isInviteFriendsDialogVisible = false,
                    isLoading = false
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 파티 초대 거절 ( API 호출은 없고 단순히 초대 목록에서 지우는 용도)
    fun onDeclineInvite(requestId: String) = intent {
        val updatedRequests = state.inviteRequests.filter { it.id != requestId }
        reduce { state.copy(inviteRequests = updatedRequests) }
    }

    // 파티 나가기
    fun onLeaveParty(isCreateParty: Boolean = true) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            leavePartyUseCase.invoke()
            val result = observeLeavePartyUseCase.invoke().first()
            reduce {
                state.copy(
                    isMenuExpanded = false,
                    partyMembers = emptyList(),
                    partyInfo = null,
                    isPartyLeader = false,
                    canStartGame = false,
                    isReady = false,
                    isLoading = false
                )
            }
            if (result.leftMemberId == authManager.getMemberId() && isCreateParty) {
                createParty()
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 게임 준비 or 준비 취소
    fun onToggleReady() = intent {
        if (state.isPartyLeader) return@intent
        reduce { state.copy(isLoading = true) }
        try {
            if(!state.isReady) readyGameUseCase.invoke()
            else unReadyGameUseCase.invoke()

            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    fun observeReadyEvent(myId: Long?) = intent {
        observeReadyGameUseCase.invoke()
            .collect { result ->
                val updatedMembers = state.partyMembers.map { member ->
                    if (member.id == result.memberId) {
                        member.copy(isReady = result.isReady)
                    } else member
                }
                reduce { state.copy(partyMembers = updatedMembers) }
                if(state.isReady) updateGameStartAvailability()
                else if(myId == result.memberId) reduce { state.copy(isReady = result.isReady) }
            }
    }

    // 게임 시작 가능 여부 업데이트
    private fun updateGameStartAvailability() = intent {
        val allReady = state.partyMembers.all { it.isReady }
        reduce { state.copy(canStartGame = allReady && state.partyMembers.isNotEmpty()) }
    }

    // 게임 시작
    fun onStartGame() = intent {
        if (!state.isPartyLeader || !state.canStartGame || state.isSearchingGame) return@intent
        try {
            readyGameUseCase.invoke()
            startGameUseCase.invoke()
        } catch (e: Exception) {
            reduce { state.copy(isSearchingGame = false, matchmakingTimeSeconds = 0) }
        }
    }

    // TODO: 게임 시작 observe
    fun observeGameStart() = intent {
        observeStartGameUseCase.invoke().collect { result ->
            when(result.status){
                DreamStatus.RECEIVED -> {}
                DreamStatus.START_MATCH -> {
                    startMatchmakingTimer()
                }
                DreamStatus.WAITING -> {}
                DreamStatus.MATCHED -> {}
                DreamStatus.CREATE_ROOM -> {}
                DreamStatus.START_DREAM -> {}
            }
        }
    }

    // 게임 검색 취소
    fun onCancelGameSearch() = intent {
        if (!state.isSearchingGame) return@intent
        try {
            // TODO: 게임 검색 취소 API 호출
            stopMatchmakingTimer()
            reduce { state.copy(isSearchingGame = false, matchmakingTimeSeconds = 0) }
        } catch (e: Exception) {
            stopMatchmakingTimer()
            reduce { state.copy(isSearchingGame = false, matchmakingTimeSeconds = 0) }
        }
    }

    // 매칭 타이머 시작
    private fun startMatchmakingTimer() = intent {
        matchmakingTimerJob?.cancel()
        matchmakingTimerJob = container.scope.launch {
            while (true) {
                delay(1000L)
                if (state.isSearchingGame) {
                    reduce { state.copy(matchmakingTimeSeconds = state.matchmakingTimeSeconds + 1) }
                } else {
                    break
                }
            }
        }
    }

    // 매칭 타이머 중지
    private fun stopMatchmakingTimer() {
        matchmakingTimerJob?.cancel()
        matchmakingTimerJob = null
    }

    // 프로필 조회
    private fun loadProfile() = intent {
        //todo 프로필(내 정보) 조회 API 연동
        val userProfile = UserProfile(1, "몽깅이")
        reduce { state.copy(userProfile = userProfile) }
    }

    // 프로필 열기
    fun onProfileClick() = intent { reduce { state.copy(isProfileDialogVisible = true) } }

    // 프로필 닫기
    fun onDismissProfileDialog() = intent {
        reduce {
            state.copy(
                isProfileDialogVisible = false,
                isNicknameEditMode = false,
                tempNickname = ""
            )
        }
    }

    // 닉네임 변경 버튼 클릭
    fun onEditNicknameClick() = intent {
        reduce {
            state.copy(
                isNicknameEditMode = true,
                tempNickname = state.userProfile.nickname
            )
        }
    }

    // 닉네임 변경
    fun onChangeNickname() = intent {
        if (state.tempNickname.isNotBlank()) {
            // TODO: 닉네임 변경 API 호출
            val updatedMembers = state.partyMembers.map { member ->
                if (member.id == state.userProfile.id)
                    member.copy(nickname = state.tempNickname) else member
            }
            reduce {
                state.copy(
                    userProfile = state.userProfile.copy(nickname = state.tempNickname),
                    isNicknameEditMode = false,
                    tempNickname = "",
                    partyMembers = updatedMembers
                )
            }
        }
    }

    // 닉네임 변경중 입력한 텍스트 변경 시 tempNickname에 업데이트
    fun onNicknameTextChange(nickname: String) =
        intent { reduce { state.copy(tempNickname = nickname) } }

    // 닉네임 변경 취소
    fun onCancelNicknameEdit() = intent {
        reduce {
            state.copy(
                isNicknameEditMode = false,
                tempNickname = ""
            )
        }
    }

    // 메뉴 탭 열기 or 닫기
    fun onMenuTabClick() = intent { reduce { state.copy(isMenuExpanded = !state.isMenuExpanded) } }

    // 초대 목록 다이얼 로그 열기
    fun onInviteListClick() = intent {
        reduce {
            state.copy(
                isMenuExpanded = false,
                isInviteRequestsDialogVisible = true
            )
        }
    }

    // 초대 목록 다이얼 로그 끄기
    fun onDismissInviteRequestsDialog() =
        intent { reduce { state.copy(isInviteRequestsDialogVisible = false) } }

    // 설정 다이얼 로그 열기
    fun onSettingsClick() = intent {
        reduce {
            state.copy(
                isSettingsDialogVisible = true,
                isMenuExpanded = false
            )
        }
    }

    // 설정 다이얼 로그 끄기
    fun onDismissSettingsDialog() =
        intent { reduce { state.copy(isSettingsDialogVisible = false) } }

    fun onLogout() = intent {
        // TODO: 로그아웃 API 호출
        authManager.logout(LogoutReason.UserLogout)
        unitySendManager.sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.START_REVERSE.value
        )
        delay(2000)
        postSideEffect(HomeContract.SideEffect.NavigateToLogin)
    }

    fun onDeleteAccount() = intent {
        // TODO: 회원탈퇴 API 호출
        postSideEffect(HomeContract.SideEffect.NavigateToLogin)
    }

    // 파티 나가기 확인 다이얼 로그 표시
    fun onShowLeavePartyDialog() = intent {
        reduce {
            state.copy(
                dialogState = DialogState.TwoButtonDialog(
                    title = "파티 나가기",
                    content = "정말로 파티를 나가시겠습니까?",
                    onConfirm = { onLeaveParty() },
                    onCancel = { hideDialog() }
                ),
                isMenuExpanded = false
            )
        }
    }

    //다이얼 로그 숨기기
    fun hideDialog() = intent { reduce { state.copy(dialogState = DialogState.Hidden) } }

    override fun onCleared() {
        super.onCleared()
//        onLeaveParty(false)
        stopMatchmakingTimer()
    }
}