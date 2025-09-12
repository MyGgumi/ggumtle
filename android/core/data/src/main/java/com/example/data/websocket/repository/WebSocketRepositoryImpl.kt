package com.ggumtle.data.websocket.repository

import android.util.Log
import com.ggumtle.data.websocket.model.social.request.AcceptFriendRequestDto
import com.ggumtle.data.websocket.model.social.request.RejectFriendRequestDto
import com.ggumtle.data.websocket.model.social.request.RequestFriendDto
import com.ggumtle.data.websocket.model.social.request.SearchMemberDataDto
import com.ggumtle.data.websocket.model.social.response.AcceptFriendResponseDto
import com.ggumtle.data.websocket.model.social.response.GetFriendRequestsResponseDto
import com.ggumtle.data.websocket.model.social.response.GetFriendsResponseDto
import com.ggumtle.data.websocket.model.social.response.RejectFriendResponseDto
import com.ggumtle.data.websocket.model.social.response.RequestFriendResponseDto
import com.ggumtle.data.websocket.model.social.response.SearchMemberDataResponseDto
import com.ggumtle.data.websocket.model.social.response.toEvent
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import kotlinx.serialization.json.Json
import javax.inject.Inject
import com.ggumtle.data.websocket.remote.datasource.WebSocketRemoteDataSource
import com.ggumtle.data.websocket.model.common.WebSocketMessageType
import com.ggumtle.data.websocket.model.home.request.AcceptPartyInvitationRequestDto
import com.ggumtle.data.websocket.model.home.request.InvitePartyRequestDto
import com.ggumtle.data.websocket.model.home.response.AcceptPartyInvitationResponseDto
import com.ggumtle.data.websocket.model.home.response.CreatePartyResponseDto
import com.ggumtle.data.websocket.model.home.response.GetInvitationsResponseDto
import com.ggumtle.data.websocket.model.home.response.InvitePartyResponseDto
import com.ggumtle.data.websocket.model.home.response.LeavePartyResponseDto
import com.ggumtle.data.websocket.model.home.response.MatchingCancelledResponseDto
import com.ggumtle.data.websocket.model.home.response.ReadyGameResponseDto
import com.ggumtle.data.websocket.model.home.response.StartGameResponseDto
import com.ggumtle.data.websocket.model.home.response.toEvent
import com.ggumtle.domain.websocket.event.EventBus
import com.ggumtle.domain.websocket.model.WebSocketEvent
import com.ggumtle.domain.websocket.repository.WebSocketRepository
import com.ggumtle.network.websocket.model.WebSocketReceiveMessageDto
import com.ggumtle.network.websocket.model.WebSocketConnectionState
import com.ggumtle.network.websocket.model.WebSocketSendMessageDto
import kotlinx.serialization.json.decodeFromJsonElement
import kotlinx.serialization.json.encodeToJsonElement

class WebSocketRepositoryImpl @Inject constructor(
    private val remoteDataSource: WebSocketRemoteDataSource,
    private val eventBus: EventBus
) : WebSocketRepository {

    private val json = Json {
        ignoreUnknownKeys = true
        encodeDefaults = false
    }

    init {
        remoteDataSource.connectionState
            .onEach { state ->
                Log.d("WebSocketRepository", "Connection state: $state")
                val event = when (state) {
                    WebSocketConnectionState.CONNECTED -> WebSocketEvent.Connected
                    WebSocketConnectionState.CONNECTING -> WebSocketEvent.Connecting
                    WebSocketConnectionState.DISCONNECTED -> WebSocketEvent.Disconnected
                    WebSocketConnectionState.FAILED -> WebSocketEvent.Failed(null)
                }
                eventBus.emit(event)
            }
            .launchIn(CoroutineScope(Dispatchers.IO + SupervisorJob()))

        remoteDataSource.messages
            .onEach { message ->
                parseAndEmitEvent(message)
            }
            .launchIn(CoroutineScope(Dispatchers.IO + SupervisorJob()))
    }

    private suspend fun parseAndEmitEvent(message: WebSocketReceiveMessageDto) {
        try {
            if(!message.success){
                Log.d("WebSocketRepository", "code: ${message.code}")
                Log.d("WebSocketRepository", "type: ${message.type}")
                Log.d("WebSocketRepository", "data: ${message.data}")
                return
            }
            when (message.type) {
                WebSocketMessageType.SEARCH_MEMBER_RESULT -> handleMessage<SearchMemberDataResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.REQUEST_FRIEND_RESULT -> handleMessage<RequestFriendResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.GET_FRIEND_REQUESTS_RESULT -> handleMessage<GetFriendRequestsResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.GET_FRIENDS_RESULT -> handleMessage<GetFriendsResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.ACCEPT_FRIEND_REQUEST_RESULT -> handleMessage<AcceptFriendResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.REJECT_FRIEND_REQUEST_RESULT -> handleMessage<RejectFriendResponseDto>(message) { it.toEvent() }
                WebSocketMessageType.CREATE_PARTY_RESULT -> handleMessage<CreatePartyResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.INVITE_PARTY_RESULT -> handleMessage<InvitePartyResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.GET_INVITATIONS_RESULT -> handleMessage<GetInvitationsResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.ACCEPT_PARTY_INVITATION_RESULT -> handleMessage<AcceptPartyInvitationResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.LEAVE_PARTY_RESULT -> handleMessage<LeavePartyResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.START_DREAM_RESULT -> handleMessage<StartGameResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.READY_DREAM_RESULT -> handleMessage<ReadyGameResponseDto>(message){ it.toEvent() }
                WebSocketMessageType.MATCHING_CANCELLED_RESULT -> handleMessage<MatchingCancelledResponseDto>(message){ it.toEvent() }
                else -> {
                    Log.d("WebSocketRepository", "Ignored message type: ${message.type}")
                }
            }
        } catch (e: Exception) {
            Log.e("WebSocketRepository", "Failed to parse message", e)
        }
    }

    private suspend inline fun <reified T> handleMessage(
        message: WebSocketReceiveMessageDto,
        transform: (T) -> WebSocketEvent?
    ) {
        message.data?.let { data ->
            val response = json.decodeFromJsonElement<T>(data)
            transform(response)?.let { eventBus.emit(it) }
        }
    }

    override suspend fun connect() {
        try {
            remoteDataSource.connect()
        } catch (e: Exception) {
            eventBus.emit(WebSocketEvent.Failed(e))
        }
    }

    override suspend fun disconnect() {
        remoteDataSource.disconnect()
    }

    override suspend fun searchMembers(keyword: String, page: Int, size: Int) {
        val data = SearchMemberDataDto(keyword = keyword, page = page, size = size)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.SEARCH_MEMBER,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun requestFriend(targetMemberId: Long) {
        val data = RequestFriendDto(targetMemberId)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.REQUEST_FRIEND,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun getFriendRequests() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.GET_FRIEND_REQUESTS,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun getFriends() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.GET_FRIENDS,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun acceptFriend(friendId: Long){
        val data = AcceptFriendRequestDto(friendId)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.ACCEPT_FRIEND_REQUEST,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun rejectFriend(friendId: Long){
        val data = RejectFriendRequestDto(friendId)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.REJECT_FRIEND_REQUEST,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun createParty() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.CREATE_PARTY,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun inviteParty(inviteeId: Long){
        val data = InvitePartyRequestDto(inviteeId)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.INVITE_PARTY,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun getInvitations() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.GET_INVITATIONS,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun acceptPartyInvitation(invitationId: String){
        val data = AcceptPartyInvitationRequestDto(invitationId)
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.ACCEPT_PARTY_INVITATION,
            data = json.encodeToJsonElement(data)
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun leaveParty() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.LEAVE_PARTY,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun startGame() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.START_DREAM,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun readyGame() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.READY_DREAM,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun unReadyGame() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.UNREADY_DREAM,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }

    override suspend fun matchingCancelled() {
        val message = WebSocketSendMessageDto(
            type = WebSocketMessageType.MATCHING_CANCELLED,
            data = null
        )
        remoteDataSource.sendMessage(message)
    }
}