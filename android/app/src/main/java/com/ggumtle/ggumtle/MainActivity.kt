package com.ggumtle.ggumtle

import android.os.Bundle
import android.util.Log
import android.widget.FrameLayout
import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.ComposeView
import androidx.lifecycle.lifecycleScope
import com.example.datastore.AuthManager
import com.example.datastore.AutoLoginState
import com.example.datastore.LogoutReason
import com.example.designsystem.theme.AppTheme
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.UnityStartupManager
import com.ggumtle.ggumtle.navigation.AppNavigation
import com.unity3d.player.UnityPlayer
import com.unity3d.player.UnityPlayerGameActivity
import dagger.hilt.android.AndroidEntryPoint
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.launch
import javax.inject.Inject

@AndroidEntryPoint
//class MainActivity : ComponentActivity()
class MainActivity : UnityPlayerGameActivity()
{

    @Inject
    lateinit var authManager: AuthManager
    @Inject
    lateinit var unitySendManager: UnitySendManager
    @Inject
    lateinit var unityStartupManager: UnityStartupManager

    //todo 유니티 스크립트 수정 후 삭제 필요
    @JvmField
    val isFinishing = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        observeUnityMessages()
        addComposeOverlay()
    }

    private fun addComposeOverlay() {
        val composeView = ComposeView(this).apply {
            setContent {
                AppTheme {
                    Box(
                        modifier = Modifier
                            .fillMaxSize()
                            .background(Color.Transparent)
                    ) {
                        AutoLoginGate(
                            authManager = authManager,
                            onAutoLoginComplete = { isLoggedIn ->
                                AppNavigation(
                                    isLoggedIn = isLoggedIn,
                                    authManager = authManager,
                                    unitySendManager = unitySendManager
                                )
                            }
                        )
                    }
                }
            }
        }

        val frameLayout = findViewById<FrameLayout>(android.R.id.content)
        frameLayout.addView(
            composeView,
            FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
        )
    }

    private fun observeUnityMessages() {
        lifecycleScope.launch {
            combine(
                unitySendManager.targetFlow,
                unitySendManager.methodFlow,
                unitySendManager.paramsFlow
            ) { target, methodName, params ->
                UnityPlayer.UnitySendMessage(target, methodName, params.joinToString())
            }.collect()
        }
    }

    fun updateLoadingProgress(progress: Int, message: String) {
        lifecycleScope.launch {
            unityStartupManager.updateProgress(progress, message)
        }
    }

    fun hideLoadingScreen() {
        lifecycleScope.launch {
            unityStartupManager.completeLoading()
        }
    }

    fun onLoadingError(errorMessage: String) {
        lifecycleScope.launch {
            unityStartupManager.reportError(errorMessage)
        }
    }

}

@Composable
private fun AutoLoginGate(
    authManager: AuthManager,
    onAutoLoginComplete: @Composable (Boolean) -> Unit
) {
    val autoLoginState by authManager.autoLoginState.collectAsState()

    when (autoLoginState) {
        is AutoLoginState.Loading -> {
            // TODO: SplashScreen()
        }
        is AutoLoginState.Success -> {
            onAutoLoginComplete(true)
        }
        is AutoLoginState.RequireLogin -> {
            onAutoLoginComplete(false)
        }
    }
}