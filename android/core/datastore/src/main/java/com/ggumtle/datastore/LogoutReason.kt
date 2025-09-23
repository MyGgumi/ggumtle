package com.ggumtle.datastore

sealed class LogoutReason {
    object UserLogout : LogoutReason()
    object TokenExpired : LogoutReason()
    data class SessionExpired(val message: String) : LogoutReason()
    object NetworkError : LogoutReason()
    object AccountDeleted : LogoutReason()

    override fun toString(): String {
        return when (this) {
            UserLogout -> "사용자 로그아웃"
            TokenExpired -> "토큰 만료"
            is SessionExpired -> "세션 만료: $message"
            NetworkError -> "네트워크 오류"
            AccountDeleted -> "회원탈퇴"
        }
    }
}