package com.example.auth

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import kotlinx.coroutines.Job

@Composable
fun LoginScreen(
    onGoogleLoginClick: () -> Unit,
    onNavigateToMain: () -> Unit,
    modifier: Modifier = Modifier,
    isLoading: Boolean = false,
    isLoginSuccess: Boolean = false,
    isNavigating: Boolean = false
) {

    Box(
        modifier = modifier.fillMaxSize(),
    ) {
        LoginContent(
            onGoogleLoginClick = onGoogleLoginClick,
            onNavigateToMain = onNavigateToMain,
            isLoading = isLoading,
            isLoginSuccess = isLoginSuccess,
            isNavigating = isNavigating
        )
    }

}