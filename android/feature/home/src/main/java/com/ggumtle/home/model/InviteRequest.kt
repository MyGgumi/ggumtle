package com.ggumtle.home.model

data class InviteRequest(
    val id: String,
    val senderName: String,
    val partySize: Int,
    val timestamp: Long = System.currentTimeMillis()
)