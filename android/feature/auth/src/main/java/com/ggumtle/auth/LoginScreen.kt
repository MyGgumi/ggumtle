package com.ggumtle.auth

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

@Composable
fun LoginScreen(
    onGoogleLoginClick: () -> Unit,
    onNavigateToMain: () -> Unit,
    modifier: Modifier = Modifier,
    isLoading: Boolean = false,
    isLoginSuccess: Boolean = false,
    isNavigating: Boolean = false
) {
    if(!isNavigating){
        LoginContent(
            onGoogleLoginClick = onGoogleLoginClick,
            onNavigateToMain = onNavigateToMain,
            isLoading = isLoading,
            isLoginSuccess = isLoginSuccess,
        )
    }
}