package com.example.domain.model

data class Member(
    val memberId: Long,
    val nickname: String,
    val status: MemberStatus
)

enum class MemberStatus {
    PENDING, ACCEPTED, REJECTED, NONE
}