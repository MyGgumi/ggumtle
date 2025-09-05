package com.ggumtle.startup

import androidx.compose.runtime.Composable

@Composable
fun StartUpScreen(
    progress: Int = 0,
    loadingMessage: String = "로딩 중...",
    isError: Boolean = false,
    errorMessage: String = "",
) {
    StartUpContent(
        progress = progress,
        loadingMessage = loadingMessage,
        isError = isError,
        errorMessage = errorMessage,
    )
}