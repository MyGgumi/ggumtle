package com.ggumtle.domain.model

data class InviteNotification(
    val message: String,
    val senderName: String = "",
    val timestamp: Long = System.currentTimeMillis(),
    val type: NotificationType = NotificationType.PARTY_INVITE
)

enum class NotificationType {
    PARTY_INVITE,
    FRIEND_REQUEST,
    GAME_START,
    SYSTEM_MESSAGE
}