package com.ggumtle.domain.websocket.model

import com.ggumtle.domain.model.Member

data class MemberSearchResult(
    val hasNext: Boolean,
    val members: List<Member>
)