package com.ggumtle.social.model

import com.ggumtle.domain.model.Member
import com.ggumtle.domain.model.MemberStatus

data class User(
    val id: Long,
    val name: String,
    val profileImage: String? = null,
    val isAlreadyFriend: Boolean = false,
    val isRequestSent: Boolean = false
)

fun List<Member>.toUsers(): List<User> {
    return this.map { it.toUser() }
}

fun Member.toUser(): User {
    return User(
        id = this.memberId,
        name = this.nickname,
        profileImage = null,
        isAlreadyFriend = this.status == MemberStatus.ACCEPTED,
        isRequestSent = this.status == MemberStatus.PENDING
    )
}