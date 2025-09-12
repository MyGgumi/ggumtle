package com.example.domain.manager

import com.example.datastore.AuthManager
import com.example.domain.model.InviteNotification
import com.example.domain.model.NotificationType
import com.example.domain.websocket.usecase.home.ObserveInvitePartyUseCase
import com.example.domain.websocket.usecase.social.ObserveRequestFriendResultUseCase
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.*
import kotlinx.coroutines.launch
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class GlobalInviteManager @Inject constructor(
    private val observeInvitePartyUseCase: ObserveInvitePartyUseCase,
    private val observeRequestFriendResultUseCase: ObserveRequestFriendResultUseCase,
    private val authManager: AuthManager
) {
    // Application 스코프에서 실행될 코루틴 스코프
    private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)
    
    // 알림 이벤트를 위한 SharedFlow
    private val _inviteNotifications = MutableSharedFlow<InviteNotification>(
        replay = 0,
        extraBufferCapacity = 10,
        onBufferOverflow = BufferOverflow.DROP_OLDEST
    )
    val inviteNotifications = _inviteNotifications.asSharedFlow()
    
    // 관찰 상태
    private var isObserving = false
    
    /**
     * 앱 시작 시 초대 관찰 시작
     */
    fun startObserving() {
        if (isObserving) return
        
        isObserving = true
        observeInviteParty()
        observeFriendRequest()

    }

    fun observeFriendRequest(){
        applicationScope.launch {
            try {
                observeRequestFriendResultUseCase.invoke()
                    .catch { error ->
                        emitNotification("네트워크 연결을 확인해주세요", type = NotificationType.SYSTEM_MESSAGE)
                    }
                    .collect { result ->
                        if(result.followerId!=authManager.getMemberId()){
                            val message = "${result.followerId}님이 파티에 초대했습니다"
                            emitNotification(
                                message = message,
                                senderName = result.followerNickname,
                                type = NotificationType.FRIEND_REQUEST
                            )
                        }
                    }
            } catch (e: Exception) {
                isObserving = false
            }
        }
    }

    fun observeInviteParty(){
        applicationScope.launch {
            try {
                observeInvitePartyUseCase.invoke()
                    .catch { error ->
                        // 에러 처리 - 로그 또는 에러 알림
                        emitNotification("네트워크 연결을 확인해주세요", type = NotificationType.SYSTEM_MESSAGE)
                    }
                    .collect { result ->
                        val message = "${result.inviterNickname}님이 파티에 초대했습니다"
                        emitNotification(
                            message = message,
                            senderName = result.inviterNickname,
                            type = NotificationType.PARTY_INVITE
                        )
                    }
            } catch (e: Exception) {
                isObserving = false
            }
        }
    }

    fun stopObserving() {
        isObserving = false
        applicationScope.coroutineContext.cancel()
    }

    fun emitNotification(
        message: String,
        senderName: String = "",
        type: NotificationType = NotificationType.PARTY_INVITE
    ) {
        val notification = InviteNotification(
            message = message,
            senderName = senderName,
            type = type
        )
        _inviteNotifications.tryEmit(notification)
    }

    fun emitGameStart() {
        emitNotification(
            message = "게임이 시작됩니다!",
            type = NotificationType.GAME_START
        )
    }
}