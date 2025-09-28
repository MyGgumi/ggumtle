package com.ggumtle.home

import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ggumtle.domain.rest.usecase.user.GetMemberInfoUseCase
import com.ggumtle.domain.rest.usecase.member.*
import com.ggumtle.domain.rest.usecase.auth.LogoutUseCase
import com.ggumtle.domain.rest.usecase.growth.GetMonggingListUseCase
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.model.MemberConnectionState
import com.ggumtle.domain.websocket.usecase.social.*
import com.ggumtle.home.model.PartyInfo
import com.ggumtle.domain.websocket.model.PartyMember
import com.ggumtle.home.model.UserProfile
import com.ggumtle.designsystem.dialog.DialogState
import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.domain.websocket.usecase.home.*
import dagger.hilt.android.lifecycle.HiltViewModel
import com.ggumtle.datastore.LogoutReason
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.websocket.model.DreamStatus
import com.ggumtle.domain.unity.UnityObserveManager
import com.ggumtle.domain.websocket.model.UnityMonggingClass
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import kotlinx.coroutines.launch
import org.orbitmvi.orbit.*
import org.orbitmvi.orbit.syntax.simple.*
import org.orbitmvi.orbit.viewmodel.container
import androidx.lifecycle.SavedStateHandle
import javax.inject.Inject
import com.ggumtle.home.model.toInviteRequests

@HiltViewModel
class HomeViewModel @Inject constructor(
    private val savedStateHandle: SavedStateHandle,
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
    private val observeStartGameUseCase: ObserveStartGameUseCase,
    private val matchingCancelledUseCase: MatchingCancelledUseCase,
    private val observeMatchingCancelledUseCase: ObserveMatchingCancelledUseCase,
    private val getPartyParticipantsUseCase: GetPartyParticipantsUseCase,
    private val observeGetPartyParticipantsUseCase: ObserveGetPartyParticipantsUseCase,
    private val getMemberInfoUseCase: GetMemberInfoUseCase,
    private val deleteAccountUseCase: DeleteAccountUseCase,
    private val editNicknameUseCase: EditNicknameUseCase,
    private val logoutUseCase: LogoutUseCase,
    private val getMonggingListUseCase: GetMonggingListUseCase,
    private val unityObserveManager: UnityObserveManager,
    private val changeMonggingTypeUseCase: ChangeMonggingTypeUseCase,
    private val observeChangeMonggingTypeUseCase: ObserveChangeMonggingTypeUseCase,
    private val getInvitationsUseCase: GetInvitationsUseCase,
    private val observeGetInvitationsUseCase: ObserveGetInvitationsUseCase
) : ViewModel(), ContainerHost<HomeContract.State, HomeContract.SideEffect> {

    override val container: Container<HomeContract.State, HomeContract.SideEffect> =
        container(HomeContract.State())

    companion object {
        private const val TAG = "HomeViewModel"
    }

    // 매칭 타이머 Job
    private var matchmakingTimerJob: Job? = null

    init {
        // 정보 조회
        loadProfile()

        // 이벤트 관찰
        observeHomeEvent()

        // 캐릭터 타입 변경 버튼 토글 관찰
        observeCharacterTypeChange()

        // 인게임 시작 네비게이션 관찰
        observeInGameStart()
    }

    // 인게임 시작 네비게이션 관찰
    private fun observeInGameStart() = intent {
        unityObserveManager.goToInGameFlow
            .onEach {
                reduce { state.copy(isGameStartingDialogVisible = false) }
                postSideEffect(HomeContract.SideEffect.NavigateToInGame)
            }
            .launchIn(viewModelScope)
    }

    // 캐릭터 타입 변경 관찰
    private fun observeCharacterTypeChange() = intent {
        unityObserveManager.characterTypeChangeFlow.collect { characterType ->
            if (state.monggings.isNotEmpty()) {
                val changedClass = state.monggings.find { it.monggingClass == characterType }
                if (changedClass == null) return@collect
                val monggingIndex = state.monggings.indexOf(changedClass)
                reduce { state.copy(selectedCharacterIndex = monggingIndex) }
                changeMonggingTypeUseCase.invoke(changedClass.id)
            }
        }
    }

    // 프로필 조회
    private fun loadProfile() = intent {
        getMemberInfoUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    val myId = authManager.getMemberId()
                    if (myId != null) {
                        authManager.saveNickname(resource.data.nickname)
                        val userProfile = UserProfile(myId, resource.data.nickname)
                        reduce {
                            state.copy(
                                userProfile = userProfile,
                                isLoading = false,
                                coin = resource.data.coin
                            )
                        }
                        loadMyMonggingList()
                    }
                }

                is Resource.Failure -> {
                    reduce { state.copy(isLoading = false) }
                    Log.e(TAG, "loadProfile: ${resource.errorMessage}")
                }
            }
        }
    }

    // 몽깅이 목록 조회
    private fun loadMyMonggingList() = intent {
        getMonggingListUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    reduce {
                        state.copy(
                            monggings = resource.data.monggings,
                            isLoading = false
                        )
                    }

                    // 로그인에서 온 경우 새 파티 생성, 다른 피처에서 온 경우 기존 파티 업데이트
                    val isFromLogin = savedStateHandle.get<Boolean>("fromLogin") ?: true
                    if (isFromLogin) {
                        createParty()
                    } else {
                        updateParty()
                    }
                }

                is Resource.Failure -> {
                    reduce { state.copy(isLoading = false) }
                    Log.e(TAG, "loadMonggingList: ${resource.errorMessage}")
                }
            }
        }
    }

    // 파티 생성
    fun createParty() = intent {
        reduce { state.copy(isLoading = true) }
        try {
            createPartyUseCase.invoke()
            val createResult = observeCreatePartyUseCase.invoke().first()
            getPartyParticipantsUseCase.invoke()
            val memberResult =
                observeGetPartyParticipantsUseCase.invoke().first { it.participants.size == 1 }
            val newPartyMember =
                memberResult.participants.find { it.id == authManager.getMemberId() }
            if (newPartyMember == null) return@intent

            unitySendManager.addMyCharacter(
                newPartyMember.nickname,
                newPartyMember.monggingLevel.toInt()
            )
            reduce {
                state.copy(
                    partyInfo = PartyInfo(partyId = createResult.partyId),
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

    private fun observeHomeEvent() {
        val myId = authManager.getMemberId()

        observeAcceptPartyInvitationEvents(myId)
        observeLeavePartyEvents(myId)
        observeReadyEvent(myId)
        observeChangeMonggingTypeEvent()
        observeGameStart()
    }

    // 몽깅이 타입 변경 관찰
    fun observeChangeMonggingTypeEvent() = intent {
        observeChangeMonggingTypeUseCase.invoke()
            .collect { result ->
                val changeMember = state.partyMembers.find { it.id == result.memberId }
                if (changeMember == null) return@collect
                unitySendManager.changeTargetCharacterType(
                    changeMember.nickname,
                    UnityMonggingClass.fromClassId(result.classId),
                    result.level.toInt()
                )
            }
    }

    // 파티 초대
    fun onInviteFriend(friendId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            invitePartyUseCase.invoke(friendId)
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

    // 파티 초대 수락
    fun onAcceptInvite(requestId: String) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            leavePartyUseCase.invoke()
            val leavePartyResult = observeLeavePartyUseCase.invoke().first()
            acceptPartyInvitationUseCase.invoke(requestId)
            val result = observeAcceptPartyInvitationUseCase.invoke()
                .first { it.joinedMemberId == authManager.getMemberId() }
            updateParty()
            reduce {
                state.copy(
                    isMenuExpanded = false,
                    isInviteRequestsDialogVisible = false,
                    partyInfo = PartyInfo(requestId),
                    isPartyLeader = false,
                    canStartGame = false,
                    isReady = false,
                    isLoading = false,
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 초대 수락 이벤트 확인 옵저브
    fun observeAcceptPartyInvitationEvents(myId: Long?) = intent {
        observeAcceptPartyInvitationUseCase.invoke()
            .collect { result ->
                if (result.joinedMemberId == myId) return@collect
                else {
                    val newPartyMember = PartyMember(
                        id = result.joinedMemberId,
                        nickname = result.joinedMemberNickname,
                        monggingLevel = result.monggingLevel,
                        monggingClassId = result.monggingClassId
                    )
                    unitySendManager.enterNewPartyMember(
                        newPartyMember.nickname,
                        UnityMonggingClass.fromClassId(newPartyMember.monggingClassId),
                        newPartyMember.monggingLevel.toInt()
                    )
                    reduce { state.copy(partyMembers = state.partyMembers + newPartyMember) }
                    updateGameStartAvailability()
                }
            }
    }

    // 파티 초대 거절 ( API 호출은 없고 단순히 초대 목록에서 지우는 용도 )
    fun onDeclineInvite(requestId: String) = intent {
        val updatedRequests = state.inviteRequests.filter { it.partyId != requestId }
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

    // 파티 나가기 옵저브
    fun observeLeavePartyEvents(myId: Long?) = intent {
        observeLeavePartyUseCase.invoke()
            .collect { result ->
                if (result.leftMemberId == myId) return@collect
                val leftMember = state.partyMembers.find { it.id == result.leftMemberId }
                val leftMemberNickname = leftMember?.nickname
                if (leftMemberNickname != null) unitySendManager.removeTargetCharacter(
                    leftMemberNickname
                )

                val updatedMembers = state.partyMembers
                    .filterNot { it.id == result.leftMemberId }
                    .map { member ->
                        when {
                            result.newLeaderId == member.id -> member.copy(isLeader = true)
                            else -> member
                        }
                    }
                if(result.newLeaderId!=null) reduce { state.copy(isPartyLeader = myId == result.newLeaderId) }
                reduce { state.copy(partyMembers = updatedMembers,) }
            }
    }

    // 파티 나가기 확인 다이얼 로그 표시
    fun onShowLeavePartyDialog() = intent {
        if (state.isSearchingGame) return@intent
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

    // 초대 가능 친구 목록 확인 다이얼 로그 열기
    fun onInviteFriendsClick() = intent {
        if (state.isSearchingGame) return@intent
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

    // 초대 목록 다이얼 로그 열기
    fun onInviteListClick() = intent {
        if (state.isSearchingGame) return@intent
        try {
            getInvitationsUseCase.invoke()
            val result = observeGetInvitationsUseCase.invoke().first()
            reduce {
                state.copy(
                    inviteRequests = result.invitations.toInviteRequests(),
                    isMenuExpanded = false,
                    isInviteRequestsDialogVisible = true
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 초대 목록 다이얼 로그 끄기
    fun onDismissInviteRequestsDialog() =
        intent { reduce { state.copy(isInviteRequestsDialogVisible = false) } }

    // 프로필 열기
    fun onProfileClick() = intent {
        if (state.isSearchingGame) return@intent
        reduce { state.copy(isProfileDialogVisible = true) }
    }

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

    // 닉네임 변경
    fun onChangeNickname() = intent {
        if (state.tempNickname.isNotBlank()) {
            reduce { state.copy(isLoading = true) }
            editNicknameUseCase.invoke(state.tempNickname).collect { resource ->
                when (resource) {
                    is Resource.Loading -> reduce { state.copy(isLoading = true) }
                    is Resource.Success -> {
                        authManager.saveNickname(state.tempNickname)

                        val updatedMembers = state.partyMembers.map { member ->
                            if (member.id == state.userProfile.id)
                                member.copy(nickname = state.tempNickname) else member
                        }

                        unitySendManager.changeTargetCharacterNickname(
                            state.userProfile.nickname,
                            state.tempNickname
                        )

                        reduce {
                            state.copy(
                                userProfile = state.userProfile.copy(nickname = state.tempNickname),
                                isNicknameEditMode = false,
                                isProfileDialogVisible = false,
                                tempNickname = "",
                                partyMembers = updatedMembers,
                                isLoading = false
                            )
                        }
                    }

                    is Resource.Failure -> {
                        reduce { state.copy(isLoading = false) }
                        Log.e(TAG, "onChangeNickname: ${resource.errorMessage}")
                    }
                }
            }
        }
    }

    // 닉네임 변경중 입력한 텍스트 변경 시 tempNickname에 업데이트
    fun onNicknameTextChange(nickname: String) =
        intent { reduce { state.copy(tempNickname = nickname) } }

    // 닉네임 변경 버튼 클릭
    fun onEditNicknameClick() = intent {
        if (state.isSearchingGame) return@intent
        reduce {
            state.copy(
                isNicknameEditMode = true,
                tempNickname = state.userProfile.nickname
            )
        }
    }

    // 닉네임 변경 취소
    fun onCancelNicknameEdit() =
        intent { reduce { state.copy(isNicknameEditMode = false, tempNickname = "") } }

    // 메뉴 탭 열기 or 닫기
    fun onMenuTabClick() = intent { reduce { state.copy(isMenuExpanded = !state.isMenuExpanded) } }

    // 설정 다이얼 로그 열기
    fun onSettingsClick() = intent {
        if (state.isSearchingGame) return@intent
        reduce { state.copy(isSettingsDialogVisible = true, isMenuExpanded = false) }
    }

    // 설정 다이얼 로그 끄기
    fun onDismissSettingsDialog() =
        intent { reduce { state.copy(isSettingsDialogVisible = false) } }

    // 로그아웃
    fun onLogout() = intent {
        if (state.isSearchingGame) return@intent
        logoutUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    authManager.logout(LogoutReason.UserLogout)
                    reduce {
                        state.copy(
                            isLoading = false,
                            isNavigating = true
                        )
                    }
                }

                is Resource.Failure -> {
                    reduce { state.copy(isLoading = false) }
                    Log.e(TAG, "onLogout: ${resource.errorMessage}")
                }
            }
        }
    }

    // 회원 탈퇴
    fun onDeleteAccount() = intent {
        if (state.isSearchingGame) return@intent
        deleteAccountUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    if (resource.data.success) authManager.logout(LogoutReason.AccountDeleted)
                    reduce {
                        state.copy(
                            isLoading = false,
                            isNavigating = true
                        )
                    }
                }

                is Resource.Failure -> {
                    reduce { state.copy(isLoading = false) }
                    Log.e(TAG, "onDeleteAccount: ${resource.errorMessage}")
                }
            }
        }
    }

    // 소셜 화면 열기
    fun onSocialClick() = intent {
        if (state.isSearchingGame) return@intent
        postSideEffect(HomeContract.SideEffect.NavigateToSocial)
    }

    // 성장 화면 열기
    fun onGrowthClick() = intent {
        if (state.isSearchingGame) return@intent
        reduce { state.copy(isNavigating = true) }
        unitySendManager.goToGrowthFromHome()
        delay(1100)
        postSideEffect(HomeContract.SideEffect.NavigateToGrowth)
    }

    //다이얼 로그 숨기기
    fun hideDialog() = intent { reduce { state.copy(dialogState = DialogState.Hidden) } }

    // 게임 준비 확인 옵저브
    fun observeReadyEvent(myId: Long?) = intent {
        observeReadyGameUseCase.invoke()
            .collect { result ->
                if (state.isPartyLeader&&result.memberId==myId) return@collect
                if (myId == result.memberId) reduce { state.copy(isReady = result.isReady) }
                val updatedMembers = state.partyMembers.map { member ->
                    if (member.id == result.memberId) {
                        member.copy(isReady = result.isReady)
                    } else member
                }
                Log.d("WebSocketReceiveMessageDto", "observeReadyEvent: ${updatedMembers}")
                reduce { state.copy(partyMembers = updatedMembers) }
                updateGameStartAvailability()
            }
    }

    // 게임 시작 관찰
    fun observeGameStart() = intent {
        // TODO: 게임 시작
        observeStartGameUseCase.invoke().collect { result ->
            when (result.status) {
                DreamStatus.RECEIVED -> {}
                DreamStatus.START_MATCH -> {
                    startMatchmakingTimer()
                    delay(5000)
                    reduce { state.copy(isGameStartingDialogVisible = true) }
                    unitySendManager.goToInGame(
                        authManager.getAccessToken().toString(),
                        -4,
                        "p-ryan.iptime.org",
                        8888
                    )
                }

                DreamStatus.WAITING -> {}
                DreamStatus.MATCHED -> {}
                DreamStatus.CREATE_ROOM -> {}
                DreamStatus.START_DREAM -> {
//                    reduce { state.copy(isGameStartingDialogVisible = true) }
//                    unitySendManager.goToInGame(
//                        authManager.getAccessToken().toString(),
//                        -4,
//                        "p-ryan.iptime.org",
//                        8888
//                    )
                }
            }
        }
    }

    // 게임 시작
    fun onStartGame() = intent {
//        unitySendManager.goToInGame(
//            authManager.getAccessToken().toString(),
//            -4,
//            "p-ryan.iptime.org",
//            8888
//        )
        if (!state.isPartyLeader || !state.canStartGame || state.isSearchingGame) return@intent
        try {
            readyGameUseCase.invoke()
            val result = observeReadyGameUseCase.invoke().first()
            startGameUseCase.invoke()
            reduce { state.copy(isSearchingGame = true) }
        } catch (e: Exception) {
            reduce { state.copy(isSearchingGame = false, matchmakingTimeSeconds = 0) }
        }
    }

    // 게임 준비 or 준비 취소
    fun onToggleReady() = intent {
        if (state.isPartyLeader) return@intent
        reduce { state.copy(isLoading = true) }
        try {
            if (!state.isReady) readyGameUseCase.invoke()
            else unReadyGameUseCase.invoke()
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 게임 시작 가능 여부 업데이트
    private fun updateGameStartAvailability() = intent {
        val allReady = state.partyMembers.all { it.isReady || it.isLeader }
        reduce { state.copy(canStartGame = allReady && state.partyMembers.isNotEmpty()) }
    }

    // 게임 검색 취소
    fun onCancelGameSearch() = intent {
        if (!state.isSearchingGame) return@intent
        try {
            matchingCancelledUseCase.invoke()
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

    private fun updateParty() = intent {
        try {
            getPartyParticipantsUseCase.invoke()
            val partyResult = observeGetPartyParticipantsUseCase.invoke().first()
            unitySendManager.updateParty(partyResult.participants, authManager.getMemberId())
            reduce {
                state.copy(
                    partyMembers = partyResult.participants,
                )
            }
            updateGameStartAvailability()
        } catch (e: Exception) {
            Log.e("HomeViewModel", "파티 정보 업데이트 실패", e)
        }
    }

    // 매칭 타이머 중지
    private fun stopMatchmakingTimer() {
        matchmakingTimerJob?.cancel()
        matchmakingTimerJob = null
    }

    override fun onCleared() {
        super.onCleared()
        onLeaveParty(false)
        stopMatchmakingTimer()
    }
}