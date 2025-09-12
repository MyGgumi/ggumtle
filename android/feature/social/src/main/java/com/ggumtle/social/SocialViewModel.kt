package com.ggumtle.social

import android.util.Log
import androidx.compose.ui.text.input.TextFieldValue
import androidx.lifecycle.ViewModel
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.model.MemberConnectionState
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
    private val observeRejectFriendRequestUseCase: ObserveRejectFriendRequestUseCase
) : ViewModel(), ContainerHost<SocialContract.State, SocialContract.SideEffect> {

    override val container: Container<SocialContract.State, SocialContract.SideEffect> =
        container(SocialContract.State())

    init {
        getFriends()
        getFriendRequests()
        observeSocialEvent()
    }

    private fun observeSocialEvent() {
        val myId = authManager.getMemberId()

        observeRequestFriendEvents(myId)
        observeAcceptFriendEvents(myId)
        observeRejectFriendEvents()
    }

    // 친구 요청 결과 관찰
    private fun observeRequestFriendEvents(myId: Long?) = intent {
        observeRequestFriendResultUseCase.invoke()
            .collect { result ->
                if (result.followerId == myId) {
                    //todo 보낸 친구 요청 목록에 추가
                    Log.d("SocialViewModel", "I sent a friend request")
                } else {
                    Log.d(
                        "SocialViewModel",
                        "Received friend request from: ${result.followerNickname}"
                    )
                    val newFriendRequest = FriendRequest(
                        friendRequestId = result.friendId,
                        memberId = result.followerId,
                        nickname = result.followerNickname
                    )
                    reduce {
                        state.copy(receivedRequests = state.receivedRequests + newFriendRequest)
                    }
                }
            }
    }

    // 친구 요청 수락 관찰
    private fun observeAcceptFriendEvents(myId: Long?) = intent {
        observeAcceptFriendRequestUseCase.invoke()
            .collect { result ->
                Log.d("SocialViewModel", "Friend request accepted: $result")

                val newFriend = if (result.followerId == myId) {
                    // 내가 보낸 요청이 수락됨
                    Friend(id = result.followeeId, nickname = result.followeeNickname)
                } else {
                    // 내가 받은 요청을 수락함
                    Friend(id = result.followerId, nickname = result.followerNickname)
                }

                reduce {
                    state.copy(friends = state.friends + newFriend)
                }
            }
    }

    // 친구 요청 거절 관찰
    private fun observeRejectFriendEvents() = intent {
        observeRejectFriendRequestUseCase.invoke()
            .collect { result ->
                Log.d("SocialViewModel", "Friend request rejected: $result")

                //todo 보낸 친구요청에서 삭제 추가
                val updatedRequests = state.receivedRequests.filter { request ->
                    request.friendRequestId != result.rejectedId
                }
                reduce {
                    state.copy(receivedRequests = updatedRequests)
                }
            }
    }

    // 친구 목록 조회
    fun getFriends() = intent {
        reduce { state.copy(isLoading = true) }
        try {

            getFriendsUseCase.invoke()

            val result = observeGetFriendsUseCase.invoke().first()
//            val modifiedFriends = result.friends.map { friend ->
//                when (friend.id) {
//                    1L, 2L -> friend.copy(connectionState = MemberConnectionState.ONLINE)
//                    3L, 4L -> friend.copy(connectionState = MemberConnectionState.INGAME)
//                    else -> friend
//                }
//            }
            reduce {
                state.copy(
                    //friends = modifiedFriends,
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

    // TODO: 친구 삭제
    fun onRemoveFriend(friendId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            val updatedFriends = state.friends//.filter { it.id != friendId }
            reduce {
                state.copy(
                    friends = updatedFriends,
                    isLoading = false
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // TODO: 보낸 친구 요청 목록 조회 구현
    fun getMyFriendRequests() = intent {}

    // TODO: 친구 요청 취소
    fun onCancelSentRequest(requestId: Long) = intent {
        reduce { state.copy(isLoading = true) }
        try {
            val updatedRequests = state.sentRequests//.filter { it.id != requestId }
            reduce {
                state.copy(
                    sentRequests = updatedRequests,
                    isLoading = false
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isLoading = false) }
        }
    }

    // TODO: 친구 프로필 조회 구현
    fun onOpenProfile(userId: Long) = intent {
    }
}
