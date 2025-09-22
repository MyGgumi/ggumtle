package com.ggumtle.mission

import android.annotation.SuppressLint
import android.view.MotionEvent
import android.view.SurfaceView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.IconButtonDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.ggumtle.mission.component.ErrorView
import com.ggumtle.mission.component.LoadingView

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
    state: MissionContract.State
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Transparent)
    ) {
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

        IconButton(
            onClick = onBackClick,
            modifier = Modifier
                .align(Alignment.TopStart)
                .padding(16.dp)
                .size(48.dp),
            colors = IconButtonDefaults.iconButtonColors(
                containerColor = Color.Transparent,
                contentColor = Color.White
            )
        ) {
            Icon(
                imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                contentDescription = "뒤로가기",
                modifier = Modifier.size(24.dp)
            )
        }

        when {
            state.isLoading -> { LoadingView(modifier = Modifier.align(Alignment.Center)) }

            state.error != null -> {
                ErrorView(
                    errorMessage = state.error,
                    modifier = Modifier.align(Alignment.Center)
                )
            }
        }
    }
}