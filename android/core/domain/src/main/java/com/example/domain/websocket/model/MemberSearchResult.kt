package com.example.domain.websocket.model

import com.example.domain.model.Member

data class MemberSearchResult(
    val hasNext: Boolean,
    val members: List<Member>
)