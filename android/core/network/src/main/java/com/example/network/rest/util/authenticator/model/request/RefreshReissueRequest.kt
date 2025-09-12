package com.ggumtle.network.rest.util.authenticator.model.request

import kotlinx.serialization.Serializable

@Serializable
data class RefreshReissueRequest(
    val refreshToken: String
)
