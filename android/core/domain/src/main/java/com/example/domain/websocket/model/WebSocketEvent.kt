package com.ggumtle.domain.websocket.model

import com.example.domain.websocket.model.MonggingClass
import com.example.domain.websocket.model.PartyMember
import com.example.domain.websocket.model.SentRequest
import com.ggumtle.domain.model.Member

sealed class WebSocketEvent {
    // 연결 상태 이벤트
    data object Connected : WebSocketEvent()
    data object Disconnected : WebSocketEvent()
    data object Connecting : WebSocketEvent()
    data class Failed(val error: Throwable?) : WebSocketEvent()

    // 소셜 이벤트
    data class MemberSearchSuccess(
        val hasNext: Boolean,
        val members: List<Member>
    ) : WebSocketEvent()

    data class RequestFriendSuccess(
        val friendId: Long,
        val followerId: Long,
        val followerNickname: String,
        val followeeId: Long,
        val followeeNickname: String
        ) : WebSocketEvent()

    data class GetFriendRequestsSuccess(
        val friendRequests: List<FriendRequest>
    ) : WebSocketEvent()

    data class GetFriendsSuccess(
        val friends: List<Friend>
    ) : WebSocketEvent()

    data class AcceptFriendRequestSuccess(
        val followerId: Long,
        val followerNickname: String,
        val followeeId: Long,
        val followeeNickname: String
    ) : WebSocketEvent()

    data class RejectFriendRequestSuccess(
        val rejectedId: Long,
        val nickname: String
    ) : WebSocketEvent()

    data class GetSentFriendRequestsSuccess(
        val sentFriendRequests: List<SentRequest>
    ) : WebSocketEvent()

    data class CancelFriendRequestSuccess(
        val friendRequestId: Long,
        val requesterId: Long,
        val nickname: String
    ) : WebSocketEvent()

    data class DeleteFriendRequestSuccess(
        val followerId: Long,
        val followerNickname: String,
        val followeeId: Long,
        val followeeNickname: String,
    ) : WebSocketEvent()

    data class GetProfileFriendSuccess(
        val memberId: Long,
        val nickname: String,
        val monggingClass: MonggingClass,
        val monggingLevel: Int,
    ) : WebSocketEvent()

    // 대기방 이벤트
    data class CreatePartySuccess(
        val partyId: String
    ) : WebSocketEvent()

    data class InvitePartySuccess(
        val invitationId: String,
        val inviterNickname: String
    ) : WebSocketEvent()

    data class GetInvitationsSuccess(
        val invitations: List<Invitation>
    ) : WebSocketEvent()

    data class AcceptPartyInvitationSuccess(
        val joinedMemberId: Long,
        val joinedMemberNickname: String
    ) : WebSocketEvent()

    data class LeavePartySuccess(
        val leftMemberId: Long,
        val newLeaderId: Long?
    ) : WebSocketEvent()

    data class StartGameSuccess(
        val status: DreamStatus,
        val dream: Dream?
    ) : WebSocketEvent()

    data class ReadyGameSuccess(
        val memberId: Long,
        val isReady: Boolean
    ) : WebSocketEvent()

    data class MatchingCancelledSuccess(
        val message: String
    ) : WebSocketEvent()

    data class GetPartyParticipantsSuccess(
        val participants: List<PartyMember>
    ) : WebSocketEvent()
}