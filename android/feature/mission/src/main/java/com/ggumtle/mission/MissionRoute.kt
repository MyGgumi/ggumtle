package com.ggumtle.mission

import android.Manifest
import android.content.pm.PackageManager
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.camera.core.CameraSelector
import androidx.camera.core.Preview
import androidx.camera.lifecycle.ProcessCameraProvider
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.content.ContextCompat
import androidx.hilt.navigation.compose.hiltViewModel
import org.orbitmvi.orbit.compose.collectAsState
import org.orbitmvi.orbit.compose.collectSideEffect
import com.ggumtle.designsystem.dialog.DialogContainer

@Composable
fun MissionRoute(
    onNavigateBack: () -> Unit = {},
    hideUnity: () -> Unit = {},
    showUnity: () -> Unit = {}
) {
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current
    var isARActive by remember { mutableStateOf(false) }
    var hasPermission by remember { mutableStateOf(false) }

    // 권한 요청
    val permissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { isGranted ->
        hasPermission = isGranted
        if (isGranted && !isARActive) {
            hideUnity()
            isARActive = true
        } else if (!isGranted) {
            Toast.makeText(context, "카메라 권한이 필요합니다", Toast.LENGTH_SHORT).show()
        }
    }

    Column(
        modifier = Modifier.fillMaxSize()
    ) {
        // 상단 바
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = {
                if (isARActive) {
                    showUnity()
                    isARActive = false
                }
                onNavigateBack()
            }) {
                Icon(
                    imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                    contentDescription = "뒤로가기"
                )
            }
            Text(
                text = "미션",
                style = MaterialTheme.typography.headlineSmall,
                modifier = Modifier.padding(start = 8.dp)
            )
        }

        // AR 시작 버튼
        Button(
            onClick = {
                if (!isARActive) {
                    // 권한 체크
                    if (ContextCompat.checkSelfPermission(context, Manifest.permission.CAMERA)
                        == PackageManager.PERMISSION_GRANTED) {
                        hasPermission = true
                        hideUnity()
                        isARActive = true
                    } else {
                        permissionLauncher.launch(Manifest.permission.CAMERA)
                    }
                } else {
                    showUnity()
                    isARActive = false
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Text(if (isARActive) "AR 종료" else "AR 시작")
        }

        // AR 뷰 또는 기본 화면
        if (isARActive && hasPermission) {
            AndroidView(
                factory = { context ->
                    androidx.camera.view.PreviewView(context).apply {
                        val cameraProviderFuture = ProcessCameraProvider.getInstance(context)
                        cameraProviderFuture.addListener({
                            try {
                                val cameraProvider = cameraProviderFuture.get()
                                val preview = Preview.Builder().build()
                                preview.setSurfaceProvider(surfaceProvider)

                                val cameraSelector = CameraSelector.DEFAULT_BACK_CAMERA
                                cameraProvider.bindToLifecycle(
                                    lifecycleOwner,
                                    cameraSelector,
                                    preview
                                )
                            } catch (e: Exception) {
                                e.printStackTrace()
                                Toast.makeText(context, "카메라 초기화 실패: ${e.message}", Toast.LENGTH_SHORT).show()
                            }
                        }, ContextCompat.getMainExecutor(context))
                    }
                },
                modifier = Modifier.fillMaxSize()
            )
        } else {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = if (!hasPermission && isARActive) {
                        "카메라 권한이 필요합니다"
                    } else {
                        "AR 시작 버튼을 눌러주세요"
                    },
                    style = MaterialTheme.typography.bodyLarge
                )
            }
        }
    }
}