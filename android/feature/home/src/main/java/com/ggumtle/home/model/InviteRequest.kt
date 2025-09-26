package com.ggumtle.home.model

import com.ggumtle.domain.websocket.model.Invitation

data class InviteRequest(
    val partyId: String,
    val senderName: String,
//    val partySize: Int,
//    val timestamp: Long = System.currentTimeMillis()
)

// 확장 함수: Invitation을 InviteRequest로 변환
fun Invitation.toInviteRequest(): InviteRequest = InviteRequest(
    partyId = this.invitationId,
    senderName = this.inviterNickname
)

// 확장 함수: Invitation 리스트를 InviteRequest 리스트로 변환
fun List<Invitation>.toInviteRequests(): List<InviteRequest> =
    this.map { it.toInviteRequest() }