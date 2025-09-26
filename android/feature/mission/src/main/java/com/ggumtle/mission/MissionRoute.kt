package com.ggumtle.mission

import android.util.Log
import android.widget.Toast
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalContext
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect

@Composable
fun MissionRoute(
    onNavigateBack: () -> Unit,
    viewModel: MissionViewModel = hiltViewModel()
) {
    val context = LocalContext.current
    val configuration = LocalConfiguration.current
    val lifecycleOwner = LocalLifecycleOwner.current

    val state by viewModel.collectAsState()

    // 생명주기 이벤트 처리
    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            when (event) {
                Lifecycle.Event.ON_RESUME -> {
                    viewModel.resumeArSession()
                }

                Lifecycle.Event.ON_PAUSE -> {
                    viewModel.pauseArSession()
                }

                Lifecycle.Event.ON_START -> {
                    viewModel.startUx()
                }

                Lifecycle.Event.ON_STOP -> {
                    viewModel.cancelStartScope()
                }

                Lifecycle.Event.ON_DESTROY -> {
                    viewModel.cancelCreateScope()
                }

                else -> {}
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            Log.d("MissionRoute", "생명주기 observer 해제")
            lifecycleOwner.lifecycle.removeObserver(observer)
        }
    }

    // 화면 회전 감지
    LaunchedEffect(configuration) {
        viewModel.initAR(context)
        viewModel.onConfigurationChanged(configuration)
    }

    // 사이드 이펙트 처리
    viewModel.collectSideEffect { sideEffect ->
        Log.d("MissionRoute", "사이드 이펙트: ${sideEffect::class.simpleName}")
        when (sideEffect) {
            is MissionContract.SideEffect.ShowToast -> {
                Toast.makeText(context, sideEffect.message, Toast.LENGTH_SHORT).show()
            }

            is MissionContract.SideEffect.ShowError -> {
                Log.e("MissionRoute", "AR 에러: ${sideEffect.message}")
                // TODO: 에러 표시
            }

            is MissionContract.SideEffect.NavigateToGrowth -> {
                Log.d("MissionRoute", "액티비티 종료")
                onNavigateBack()
            }
        }
    }

    MissionScreen(
        onSurfaceViewReady = { surfaceView ->
            viewModel.setSurfaceView(surfaceView)
        },
        onTouchEvent = { motionEvent ->
            viewModel.onTouchEvent(context, motionEvent)
        },
        onBackClick = viewModel::onBackClick,
        onCapturePhoto = {
            viewModel.capturePhoto(context)
        },
        onImagePreviewClick = viewModel::showImageDialog,
        onHideImageDialog = viewModel::hideImageDialog,
        onShowRemainMissionDialog = viewModel::showRemainMissionDialog,
        onHideRemainMissionDialog = viewModel::hideRemainMissionDialog,
        onShowEmoticonSelector = viewModel::showEmoticonSelector,
        onHideEmoticonSelector = viewModel::hideEmoticonSelector,
        onEmoticonSelected = viewModel::playEmoticonAnimation,
        // 먹이주기 관련
        onFoodDragStart = viewModel::startFoodDrag,
        onFoodDragEnd = viewModel::endFoodDrag,
        onFoodDrag = viewModel::updateFoodPosition,
        state = state
    )
}