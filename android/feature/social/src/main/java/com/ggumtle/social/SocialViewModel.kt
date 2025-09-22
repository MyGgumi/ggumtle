package com.ggumtle.social

import android.util.Log
import androidx.compose.ui.text.input.TextFieldValue
import androidx.lifecycle.ViewModel
import com.ggumtle.domain.websocket.model.SentRequest
import com.ggumtle.domain.websocket.usecase.social.CancelFriendRequestUseCase
import com.ggumtle.domain.websocket.usecase.social.DeleteFriendUseCase
import com.ggumtle.domain.websocket.usecase.social.GetProfileFriendUseCase
import com.ggumtle.domain.websocket.usecase.social.GetSentFriendRequestsUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveCancelFriendRequestUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveDeleteFriendUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveGetProfileFriendUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveGetSentFriendRequestsUseCase
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.websocket.model.Friend
import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.domain.websocket.usecase.social.GetFriendRequestsUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveGetFriendRequestsUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveMemberSearchResultUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveRequestFriendResultUseCase
import com.ggumtle.domain.websocket.usecase.social.RequestFriendUseCase
import com.ggumtle.domain.websocket.usecase.social.SearchMembersUseCase
import com.ggumtle.domain.websocket.usecase.social.AcceptFriendRequestUseCase
import com.ggumtle.domain.websocket.usecase.social.GetFriendsUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveAcceptFriendRequestUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveGetFriendsUseCase
import com.ggumtle.domain.websocket.usecase.social.ObserveRejectFriendRequestUseCase
import com.ggumtle.domain.websocket.usecase.social.RejectFriendRequestUseCase
import com.ggumtle.social.model.toUsers
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.onCompletion
import kotlinx.coroutines.flow.catch
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject
import kotlin.collections.plus

@HiltViewModel
class SocialViewModel @Inject constructor(
    private val authManager: AuthManager,
    private val searchMembersUseCase: SearchMembersUseCase,
    private val observeMemberSearchResultUseCase: ObserveMemberSearchResultUseCase,
    private val requestFriendUseCase: RequestFriendUseCase,
    private val observeRequestFriendResultUseCase: ObserveRequestFriendResultUseCase,
    private val getFriendRequestsUseCase: GetFriendRequestsUseCase,
    private val observeGetFriendRequestsUseCase: ObserveGetFriendRequestsUseCase,
    private val getFriendsUseCase: GetFriendsUseCase,
    private val observeGetFriendsUseCase: ObserveGetFriendsUseCase,
    private val acceptFriendRequestUseCase: AcceptFriendRequestUseCase,
    private val observeAcceptFriendRequestUseCase: ObserveAcceptFriendRequestUseCase,
    private val rejectFriendRequestUseCase: RejectFriendRequestUseCase,
    private val observeRejectFriendRequestUseCase: ObserveRejectFriendRequestUseCase,
    private val getSentFriendRequestsUseCase: GetSentFriendRequestsUseCase,
    private val observeGetSentFriendsUseCase: ObserveGetSentFriendRequestsUseCase,
    private val deleteFriendUseCase: DeleteFriendUseCase,
    private val observeDeleteFriendUseCase: ObserveDeleteFriendUseCase,
    private val cancelFriendRequestUseCase: CancelFriendRequestUseCase,
    private val observeCancelFriendRequestUseCase: ObserveCancelFriendRequestUseCase,
    private val getProfileFriendUseCase: GetProfileFriendUseCase,
    private val observeGetProfileFriendUseCase: ObserveGetProfileFriendUseCase
) : ViewModel(), ContainerHost<SocialContract.State, SocialContract.SideEffect> {

    override val container: Container<SocialContract.State, SocialContract.SideEffect> =
        container(SocialContract.State())

    init {
        Log.d("SocialViewModel", "ViewModel 생성됨")
        getFriends()
        getFriendRequests()
        getMyFriendRequests()
        observeSocialEvent()
    }

    override fun onCleared() {
        super.onCleared()
        Log.d("SocialViewModel", "ViewModel 종료됨 - onCleared() 호출")
    }

    private fun observeSocialEvent() {
        val myId = authManager.getMemberId()

        observeRequestFriendEvents(myId)
        observeAcceptFriendEvents(myId)
        observeRejectFriendEvents()
        observeCancelFriendRequestEvents()
        observeDeleteFriendEvents()
    }

    // 친구 요청 결과 관찰
    private fun observeRequestFriendEvents(myId: Long?) = intent {
        Log.d("SocialViewModel", "observeRequestFriendEvents 시작")
        observeRequestFriendResultUseCase.invoke()
            .onCompletion { cause ->
                Log.d("SocialViewModel", "observeRequestFriendEvents 완료됨, cause: $cause")
            }
            .catch { throwable ->
                Log.e("SocialViewModel", "observeRequestFriendEvents 에러 발생", throwable)
            }
            .collect { result ->
                if (result.followerId == myId) {
                    val newSentFriendRequest = SentRequest(
                        id = result.friendId,
                        toUserId = result.followeeId,
                        toUserName = result.followeeNickname
                    )
                    reduce { state.copy(sentRequests = state.sentRequests + newSentFriendRequest) }
                } else {
                    val newFriendRequest = FriendRequest(
                        friendRequestId = result.friendId,
                        memberId = result.followerId,
                        nickname = result.followerNickname
                    )
                    reduce { state.copy(receivedRequests = state.receivedRequests + newFriendRequest) }
                }
            }
    }

    // 친구 요청 수락 관찰
    private fun observeAcceptFriendEvents(myId: Long?) = intent {
        Log.d("SocialViewModel", "observeAcceptFriendEvents 시작")
        observeAcceptFriendRequestUseCase.invoke()
            .collect { result ->
                val newFriend = if (result.followerId == myId) {
                    // 내가 보낸 요청이 수락됨
                    val updateSentRequests = state.sentRequests.filter { it.toUserId != result.followeeId }
                    reduce { state.copy(sentRequests = updateSentRequests) }
                    Friend(id = result.followeeId, nickname = result.followeeNickname)
                } else {
                    // 내가 받은 요청을 수락함
                    Friend(id = result.followerId, nickname = result.followerNickname)
                }

                reduce { state.copy(friends = state.friends + newFriend) }
            }
    }

    // 친구 요청 거절 관찰
    private fun observeRejectFriendEvents() = intent {
        Log.d("SocialViewModel", "observeRejectFriendEvents 시작")
        observeRejectFriendRequestUseCase.invoke()
            .collect { result ->
                val updatedReceivedRequests = state.receivedRequests.filter { request ->
                    request.friendRequestId != result.rejectedId
                }
                val updatedSentRequests = state.sentRequests.filter { request ->
                    request.id != result.rejectedId
                }
                reduce {
                    state.copy(
                        receivedRequests = updatedReceivedRequests,
                        sentRequests = updatedSentRequests
                    )
                }
            }
    }

    // 친구 요청 취소 관찰
    private fun observeCancelFriendRequestEvents() = intent {
        Log.d("SocialViewModel", "observeCancelFriendRequestEvents 시작")
        observeCancelFriendRequestUseCase.invoke()
            .collect { result ->
                val updatedReceivedRequests = state.receivedRequests.filter { request ->
                    request.friendRequestId != result.friendRequestId
                }
                val updatedSentRequests = state.sentRequests.filter { request ->
                    request.id != result.friendRequestId
                }
                reduce {
                    state.copy(
                        receivedRequests = updatedReceivedRequests,
                        sentRequests = updatedSentRequests
                    )
                }
            }
    }

    private fun observeDeleteFriendEvents() = intent {
        Log.d("SocialViewModel", "observeDeleteFriendEvents 시작")
        observeDeleteFriendUseCase.invoke()
            .collect { result ->
                val updateFriends = state.friends.filter { it.id != result.followerId && it.id != result.followeeId }
                reduce { state.copy(friends = updateFriends) }
            }
    }

    // 친구 목록 조회
    fun getFriends() = intent {
        reduce { state.copy(isLoading = true) }
        try {

            getFriendsUseCase.invoke()

            val result = observeGetFriendsUseCase.invoke().first()
            reduce {
                state.copy(
                    friends = result.friends,
                    isLoading = false
                )
            }

        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 친구 요청 수락
    fun onAcceptFriendRequest(requestId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            acceptFriendRequestUseCase.invoke(requestId)
            val updatedRequests =
                state.receivedRequests.filter { request -> request.friendRequestId != requestId }
            reduce {
                state.copy(
                    isLoading = false,
                    receivedRequests = updatedRequests
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 친구 요청 거절
    fun onRejectFriendRequest(requestId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            rejectFriendRequestUseCase.invoke(requestId)
            val updatedRequests =
                state.receivedRequests.filter { request -> request.friendRequestId != requestId }
            reduce {
                state.copy(
                    isLoading = false,
                    receivedRequests = updatedRequests
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 친구 요청
    fun onSendFriendRequest(userId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            requestFriendUseCase.invoke(userId)
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 친구 요청 전송 후 다이얼로그 닫기
    fun onSendFriendRequestAndCloseDialog(userId: Long) = intent {
        reduce { state.copy(isSearchLoading = true) }
        try {
            requestFriendUseCase.invoke(userId)
            reduce {
                state.copy(
                    isSearchLoading = false,
                    isSearchDialogVisible = false,
                    addFriendsSearchQuery = TextFieldValue(""),
                    searchResults = emptyList()
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isSearchLoading = false) }
        }
    }

    // 친구 요청 목록 확인
    private fun getFriendRequests() = intent {
        reduce { state.copy(isLoading = true) }
        try {

            getFriendRequestsUseCase.invoke()

            val result = observeGetFriendRequestsUseCase.invoke().first()

            reduce {
                state.copy(
                    receivedRequests = result.friendRequests,
                    isLoading = false
                )
            }

        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // 친구 추가를 위한 사용자 검색 필드 업데이트 시
    fun onUpdateAddFriendsSearchQuery(query: TextFieldValue) = intent {
        reduce { state.copy(addFriendsSearchQuery = query) }
        if (query.text.isNotEmpty()) {
            reduce { state.copy(isSearchLoading = true) }
            try {
                searchMembersUseCase.invoke(keyword = query.text, page = 0, size = 20)

                val result = observeMemberSearchResultUseCase.invoke().first()
                val searchResults = result.members.toUsers()

                reduce {
                    state.copy(
                        searchResults = searchResults,
                        isSearchLoading = false
                    )
                }
            } catch (e: Exception) {
                reduce { state.copy(isSearchLoading = false) }
            }
        } else {
            reduce { state.copy(searchResults = emptyList()) }
        }
    }

    fun getMyFriendRequests() = intent {
        reduce { state.copy(isLoading = true) }
        try {

            getSentFriendRequestsUseCase.invoke()

            val result = observeGetSentFriendsUseCase.invoke().first()
            Log.d("qwer", "getMyFriendRequests: ${result.sentFriendRequests}")
            reduce {
                state.copy(
                    sentRequests = result.sentFriendRequests,
                    isLoading = false
                )
            }

        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    fun onRemoveFriend(friendId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            deleteFriendUseCase.invoke(friendId)
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    fun onCancelSentRequest(requestId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            cancelFriendRequestUseCase.invoke(requestId)
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // TODO: 친구 프로필 조회 구현
    fun onOpenProfile(userId: Long) = intent {

    }

    // 탭 전환
    fun onTabSelected(tab: SocialContract.SocialTab) = intent {
        getFriends()
        getFriendRequests()
        reduce { state.copy(currentTab = tab) }
    }

    // 친구 목록에서 검색
    fun onUpdateFriendsSearchQuery(query: TextFieldValue) = intent {
        reduce { state.copy(friendsSearchQuery = query) }
    }

    // 사용자 검색 다이얼로그 열기
    fun onShowSearchDialog() = intent {
        reduce { state.copy(isSearchDialogVisible = true) }
    }

    // 사용자 검색 다이얼로그 닫기
    fun onDismissSearchDialog() = intent {
        reduce {
            state.copy(
                isSearchDialogVisible = false,
                addFriendsSearchQuery = TextFieldValue(""),
                searchResults = emptyList()
            )
        }
    }

    fun onClickBack() = intent { postSideEffect(SocialContract.SideEffect.NavigateToHome) }
}
