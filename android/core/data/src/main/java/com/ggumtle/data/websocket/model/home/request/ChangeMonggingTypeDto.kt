package com.ggumtle.data.websocket.model.home.request

import kotlinx.serialization.Serializable

@Serializable
data class ChangeMonggingTypeDto (
    val monggingId: Long
)