package com.ggumtle.mission.component

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.unit.dp
import com.ggumtle.mission.component.button.CaptureButton

@Composable
fun CameraControls(
    onCapturePhoto: () -> Unit,
    lastCapturedImageUri: String?,
    onImagePreviewClick: () -> Unit,
    // 먹이주기 관련
    isDraggingFood: Boolean,
    foodPosition: Pair<Float, Float>?,
    onFoodDragStart: () -> Unit,
    onFoodDragEnd: () -> Unit,
    onFoodDrag: (Offset) -> Unit,
    modifier: Modifier = Modifier
) {
    Box(modifier = modifier.fillMaxSize()) {
        // 촬영 버튼
        Row(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .padding(bottom = 32.dp),
            horizontalArrangement = Arrangement.Center
        ) {
            CaptureButton(onCapturePhoto = onCapturePhoto)
        }

        // 썸네일 이미지
        lastCapturedImageUri?.let { imageUri ->
            ImageThumbnail(
                imageUri = imageUri,
                onImagePreviewClick = onImagePreviewClick,
                modifier = Modifier
                    .align(Alignment.BottomStart)
                    .padding(start = 16.dp, bottom = 32.dp)
            )
        }

        // 먹이 아이콘 (오른쪽 하단)
        FoodIcon(
            isDragging = isDraggingFood,
            position = foodPosition,
            onDragStart = onFoodDragStart,
            onDragEnd = onFoodDragEnd,
            onDrag = onFoodDrag,
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(end = 16.dp, bottom = 32.dp)
        )
    }
}