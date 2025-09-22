package com.ggumtle.domain.model

data class Member(
    val memberId: Long,
    val nickname: String,
    val status: MemberStatus
)

enum class MemberStatus {
    PENDING, ACCEPTED, REJECTED, NONE
}

enum class MemberConnectionState {
    ONLINE, OFFLINE, INGAME;

    companion object {
        fun fromString(state: String): MemberConnectionState {
            return when (state.uppercase()) {
                "ONLINE" -> ONLINE
                "OFFLINE" -> OFFLINE
                "INGAME" -> INGAME
                else -> OFFLINE
            }
        }
    }
}