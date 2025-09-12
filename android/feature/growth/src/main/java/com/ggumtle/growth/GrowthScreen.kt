package com.ggumtle.growth

import androidx.compose.runtime.Composable

@Composable
fun GrowthScreen(
    state: GrowthContract.State,
    onBackClick: () -> Unit
) {
    GrowthContent(
        state = state,
        onBackClick = onBackClick
    )
}