package com.ggumtle.startup

import android.widget.Toast
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.platform.LocalContext
import androidx.core.app.ActivityCompat.finishAffinity
import androidx.hilt.navigation.compose.hiltViewModel
import com.example.designsystem.dialog.DialogContainer
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import kotlin.system.exitProcess

@Composable
fun StartUpRoute(
    viewModel: StartUpViewModel = hiltViewModel(),
    onNavigateToLogin: () -> Unit
) {
    val state by viewModel.collectAsState()
    val context = LocalContext.current

    viewModel.collectSideEffect { sideEffect ->
        when (sideEffect) {
            is StartUpContract.SideEffect.NavigateToLogin -> onNavigateToLogin()
            is StartUpContract.SideEffect.ShowToast ->
                Toast.makeText(context, sideEffect.message, Toast.LENGTH_LONG).show()
            is StartUpContract.SideEffect.ExitApp -> exitProcess(0)

        }
    }

    StartUpScreen(
        progress = state.progress,
        loadingMessage = state.loadingMessage,
        isError = state.isError,
        errorMessage = state.errorMessage,
    )

    DialogContainer(
        dialogState = state.dialogState,
        onDismiss = { viewModel.hideDialog() }
    )
}