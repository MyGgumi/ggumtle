package com.example.domain.websocket.repository

interface WebSocketRepository {
    suspend fun connect()
    suspend fun disconnect()
    suspend fun searchMembers(keyword: String, page: Int, size: Int)
    suspend fun requestFriend(targetMemberId: Long)
    suspend fun getFriendRequests()
    suspend fun getFriends()
    suspend fun acceptFriend(friendId: Long)
    suspend fun rejectFriend(friendId: Long)
}