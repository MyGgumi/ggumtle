package com.ggumtle.data.websocket.model.social.request

import kotlinx.serialization.Serializable

@Serializable
data class SearchMemberDataDto(
    val keyword: String,
    val page: Int,
    val size: Int
)