package com.ggumtle.mission

import android.annotation.SuppressLint
import android.view.MotionEvent
import android.view.SurfaceView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.ggumtle.mission.component.CameraControls
import com.ggumtle.mission.component.ErrorView
import com.ggumtle.mission.component.ImagePreviewDialog
import com.ggumtle.mission.component.LoadingView
import com.ggumtle.mission.component.button.BackButton

/**
 * AR 화면의 메인 컴포저블 함수
 * @param onSurfaceViewReady SurfaceView가 준비되었을 때 호출되는 콜백
 * @param onTouchEvent 터치 이벤트를 처리하는 콜백
 * @param onBackClick 뒤로가기 버튼 클릭 콜백
 */
@SuppressLint("ClickableViewAccessibility")
@Composable
fun MissionScreen(
    onSurfaceViewReady: (SurfaceView) -> Unit,
    onTouchEvent: (MotionEvent) -> Boolean,
    onBackClick: () -> Unit,
    onCapturePhoto: () -> Unit,
    onImagePreviewClick: () -> Unit,
    onHideImageDialog: () -> Unit,
    onFoodDragStart: () -> Unit,
    onFoodDragEnd: () -> Unit,
    onFoodDrag: (Offset) -> Unit,
    state: MissionContract.State
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Transparent)
    ) {
        // AR SurfaceView (분리하지 않음)
        AndroidView(
            factory = { context ->
                SurfaceView(context).apply {
                    layoutParams = android.view.ViewGroup.LayoutParams(
                        android.view.ViewGroup.LayoutParams.MATCH_PARENT,
                        android.view.ViewGroup.LayoutParams.MATCH_PARENT
                    )
                    setOnTouchListener { _, motionEvent ->
                        onTouchEvent(motionEvent)
                    }
                    onSurfaceViewReady(this)
                }
            },
            modifier = Modifier
                .fillMaxSize()
                .align(Alignment.Center)
        )

        // 뒤로가기 버튼
        BackButton(
            onClick = onBackClick,
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(16.dp)
        )

        // 카메라 컨트롤들
        CameraControls(
            onCapturePhoto = onCapturePhoto,
            lastCapturedImageUri = state.lastCapturedImageUri,
            onImagePreviewClick = onImagePreviewClick,
            // 먹이주기 관련
            isDraggingFood = state.isDraggingFood,
            foodPosition = state.foodPosition,
            onFoodDragStart = onFoodDragStart,
            onFoodDragEnd = onFoodDragEnd,
            onFoodDrag = onFoodDrag
        )

        // 로딩 및 에러 상태
        when {
            state.isLoading || !state.isModelPlacementReady -> {
                LoadingView(modifier = Modifier.align(Alignment.Center))
            }
            state.error != null -> {
                ErrorView(
                    errorMessage = state.error,
                    modifier = Modifier.align(Alignment.Center)
                )
            }
        }
    }

    // 이미지 미리보기 다이얼로그
    if (state.showImageDialog && state.lastCapturedImageUri != null) {
        ImagePreviewDialog(
            imageUri = state.lastCapturedImageUri,
            hasModel = state.hasModelInLastImage,
            onDismiss = onHideImageDialog
        )
    }
}