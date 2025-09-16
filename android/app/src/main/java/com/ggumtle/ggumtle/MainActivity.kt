package com.ggumtle.ggumtle

import android.os.Bundle
import android.util.Log
import android.widget.FrameLayout
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import kotlinx.coroutines.delay
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.ComposeView
import androidx.lifecycle.lifecycleScope
import com.ggumtle.datastore.AuthManager
import com.ggumtle.datastore.AutoLoginState
import com.ggumtle.designsystem.component.GlobalNotificationOverlay
import com.ggumtle.designsystem.theme.AppTheme
import com.ggumtle.domain.manager.GlobalInviteManager
import com.ggumtle.domain.model.InviteNotification
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.UnityStartupManager
import com.ggumtle.ggumtle.navigation.AppNavigation
import com.ggumtle.ggumtle.unity.UnitySendManagerImpl
import com.unity3d.player.UnityPlayer
import com.unity3d.player.UnityPlayerGameActivity
import dagger.hilt.android.AndroidEntryPoint
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
    @Inject
    lateinit var globalInviteManager: GlobalInviteManager

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
                    // 전역 알림 상태
                    var notification by remember { mutableStateOf<InviteNotification?>(null) }
                    
                    // 알림 관찰
                    LaunchedEffect(Unit) {
                        globalInviteManager.inviteNotifications.collect { inviteNotification ->
                            notification = inviteNotification
                            delay(3000) // 3초 후 자동 사라짐
                            notification = null
                        }
                    }
                    
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
                        
                        // 전역 알림 오버레이
                        GlobalNotificationOverlay(
                            notification = notification,
                            onDismiss = { notification = null },
                            onTap = { 
                                // 알림 클릭 시 초대목록 다이얼로그 열기 등 추가 가능
                                notification = null
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
            (unitySendManager as UnitySendManagerImpl).unityMessageFlow.collect { message ->
                Log.d("MainActivity", "Unity로 메시지 전송: ${message.target}.${message.methodName}(${message.params.joinToString()})")
                UnityPlayer.UnitySendMessage(message.target, message.methodName, message.params.joinToString())
            }
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

    fun onNameTagClicked(nickname: String, type: String){
        lifecycleScope.launch {
            // TODO: 네임테그 클릭시 정보 다이얼로그 띄우기
        }
    }

    fun onRefreshButtonClicked(type: String){
        lifecycleScope.launch {
            // TODO: 타입 변경 버튼 호출시 타입변경 로직
        }
    }

    fun onInGameSceneLoadingProgress(progress: Float, message: String){
        lifecycleScope.launch {
            // TODO: 인게임 로딩 정보 업데이트
        }
    }

    fun onInGameSceneLoadingComplete(){
        lifecycleScope.launch {
            // TODO: 인게임 로딩 완료 업데이트
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