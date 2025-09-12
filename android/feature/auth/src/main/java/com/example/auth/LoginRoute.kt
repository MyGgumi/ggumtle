package com.ggumtle.auth

import android.widget.Toast
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.platform.LocalContext
import androidx.hilt.navigation.compose.hiltViewModel
import com.ggumtle.designsystem.dialog.DialogContainer
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect

@Composable
fun LoginRoute(
    viewModel: LoginViewModel = hiltViewModel(),
    onNavigateToMain: () -> Unit,
    isLoggedIn: Boolean = false
) {
    val state by viewModel.collectAsState()
    val context = LocalContext.current

    LaunchedEffect(isLoggedIn) {
        viewModel.setLoginStatus(isLoggedIn)
    }

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is LoginContract.SideEffect.NavigateToMain -> onNavigateToMain()
            is LoginContract.SideEffect.ShowToast ->
                Toast.makeText(context, sideEffect.message, Toast.LENGTH_SHORT).show()
        }
    }

    LoginScreen(
        onNavigateToMain = { viewModel.navigateToMain() },
        onGoogleLoginClick = { viewModel.performGoogleLogin(context) },
        isLoading = state.isLoading,
        isLoginSuccess = state.isLoginSuccess,
        isNavigating = state.isNavigating
    )

    DialogContainer(
        dialogState = state.dialogState,
        onDismiss = { viewModel.hideDialog() }
    )
}