package com.ggumtle.mission

import android.Manifest
import android.content.pm.PackageManager
import android.util.Log
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AlertDialog
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import androidx.hilt.navigation.compose.hiltViewModel
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import com.ggumtle.mission.ar.util.cameraPermissionRequestCode
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect

@Composable
fun MissionRoute(
    onNavigateBack: () -> Unit,
    viewModel: MissionViewModel = hiltViewModel()
) {
    val context = LocalContext.current
    val activity = context as ComponentActivity
    val configuration = LocalConfiguration.current
    val lifecycleOwner = LocalLifecycleOwner.current

    val state by viewModel.collectAsState()

    Log.d("MissionRoute", "MissionRoute 시작 - isLoading=${state.isLoading}, isArSessionReady=${state.isArSessionReady}")

    // 카메라 권한 요청 런처
    val cameraPermissionLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestPermission()
    ) { isGranted ->
        Log.d("MissionRoute", "카메라 권한 결과: $isGranted")
        val grantResults = intArrayOf(
            if (isGranted) PackageManager.PERMISSION_GRANTED
            else PackageManager.PERMISSION_DENIED
        )
        viewModel.onPermissionResult(cameraPermissionRequestCode, grantResults)
    }

    // 생명주기 이벤트 처리
    DisposableEffect(lifecycleOwner) {
        Log.d("MissionRoute", "생명주기 observer 등록")

        val observer = LifecycleEventObserver { _, event ->
            Log.d("MissionRoute", "생명주기 이벤트: $event")
            when (event) {
                Lifecycle.Event.ON_RESUME -> {
                    viewModel.onActivityResume()
                }
                Lifecycle.Event.ON_PAUSE -> {
                    viewModel.onActivityPause()
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
        Log.d("MissionRoute", "화면 회전 감지")
        viewModel.onConfigurationChanged(configuration)
    }

    // 사이드 이펙트 처리
    viewModel.collectSideEffect { sideEffect ->
        Log.d("MissionRoute", "사이드 이펙트: ${sideEffect::class.simpleName}")
        when (sideEffect) {
            is MissionContract.SideEffect.ShowToast -> {
                Log.d("MissionRoute", "토스트 메시지: ${sideEffect.message}")
            }

            is MissionContract.SideEffect.ShowPermissionDialog -> {
                Log.d("MissionRoute", "권한 다이얼로그 표시")
                if (ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA)
                    != PackageManager.PERMISSION_GRANTED) {

                    AlertDialog.Builder(context)
                        .setTitle("카메라 권한 필요")
                        .setMessage(sideEffect.message)
                        .setPositiveButton("확인") { _, _ ->
                            cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
                        }
                        .setNegativeButton("취소") { _, _ ->
                            onNavigateBack()
                        }
                        .setCancelable(false)
                        .show()
                }
            }

            is MissionContract.SideEffect.ShowOpenGlNotSupportedDialog -> {
                Log.e("MissionRoute", "OpenGL 미지원: ${sideEffect.message}")
                AlertDialog.Builder(context)
                    .setTitle("지원되지 않는 기기")
                    .setMessage(sideEffect.message)
                    .setPositiveButton("확인") { _, _ ->
                        onNavigateBack()
                    }
                    .setCancelable(false)
                    .show()
            }

            is MissionContract.SideEffect.RequestCameraPermission -> {
                Log.d("MissionRoute", "카메라 권한 요청")
                cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
            }

            is MissionContract.SideEffect.NavigateToGrowth -> {
                Log.d("MissionRoute", "액티비티 종료")
                onNavigateBack()
            }
        }
    }

    when {
        state.isLoading -> {
            Log.d("MissionRoute", "로딩 화면 표시")
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        }

        state.errorMessage != null -> {
            Log.e("MissionRoute", "에러 화면 표시: ${state.errorMessage}")
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text(text = state.errorMessage!!)
            }
        }

        else -> {
            Log.d("MissionRoute", "AR 화면 표시")
            MissionScreen(
                onSurfaceViewReady = { surfaceView ->
                    Log.d("MissionRoute", "SurfaceView 준비 완료: $surfaceView")
                    viewModel.onSurfaceViewReady(activity, surfaceView)
                },
                onTouchEvent = { motionEvent ->
                    viewModel.onTouchEvent(motionEvent)
                    true
                },
                onBackClick = viewModel::onBackClick
            )
        }
    }
}