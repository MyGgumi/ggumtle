package com.ggumtle.ggumtle.navigation

import kotlinx.serialization.Serializable

@Serializable
data object LoginDestination

@Serializable
data object StartUpDestination

@Serializable
data class HomeDestination(val fromLogin: Boolean = true)

@Serializable
data object SocialDestination

@Serializable
data object GrowthDestination

@Serializable
data object MissionDestination

@Serializable
data object InGameDestination