package com.ggumtle.mission

import android.annotation.SuppressLint
import android.view.SurfaceView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.IconButtonDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView

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
    onTouchEvent: (android.view.MotionEvent) -> Boolean,
    onBackClick: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black)
    ) {
        // AR 렌더링을 위한 SurfaceView 설정
        AndroidView(
            factory = { context ->
                SurfaceView(context).apply {
                    tag = "AR_SURFACE_VIEW" // 이 줄 추가
                    layoutParams = android.view.ViewGroup.LayoutParams(
                        android.view.ViewGroup.LayoutParams.MATCH_PARENT,
                        android.view.ViewGroup.LayoutParams.MATCH_PARENT
                    )
                    // 터치 이벤트 리스너 설정
                    setOnTouchListener { _, motionEvent ->
                        onTouchEvent(motionEvent)
                    }
                }
            },
            update = { view ->
                onSurfaceViewReady(view)
            },
            modifier = Modifier
                .fillMaxSize()
                .align(Alignment.Center)
        )

        // 뒤로가기 버튼 오버레이
        IconButton(
            onClick = onBackClick,
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(16.dp)
                .size(48.dp),
            colors = IconButtonDefaults.iconButtonColors(
                containerColor = Color.Black.copy(alpha = 0.6f),
                contentColor = Color.White
            )
        ) {
            Icon(
                imageVector = Icons.Default.ArrowBack,
                contentDescription = "뒤로가기",
                modifier = Modifier.size(24.dp)
            )
        }
    }
}