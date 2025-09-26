package com.ggumtle.mission.component

import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.spring
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import com.ggumtle.core.designsystem.R
import kotlin.math.roundToInt

@Composable
fun FoodIcon(
    isDragging: Boolean,
    position: Pair<Float, Float>?,
    onDragStart: () -> Unit,
    onDragEnd: () -> Unit,
    onDrag: (Offset) -> Unit,
    modifier: Modifier = Modifier
) {

    // 크기 애니메이션 효과
    val scale by animateFloatAsState(
        targetValue = if (isDragging) 1.2f else 1.0f,
        animationSpec = spring(dampingRatio = 0.8f),
        label = "food_scale"
    )

    Box(
        modifier = modifier
            .let { mod ->
                if (position != null && isDragging) {
                    mod.offset {
                        IntOffset(
                            position.first.roundToInt(),
                            position.second.roundToInt()
                        )
                    }
                } else mod
            }
            .size(80.dp)
            .scale(scale)
            .background(Color.Transparent)
            .pointerInput(Unit) {
                detectDragGestures(
                    onDragStart = { offset ->
                        onDragStart()
                    },
                    onDragEnd = {
                        onDragEnd()
                    },
                    onDrag = { change, dragAmount ->
                        onDrag(change.position)
                    }
                )
            },
        ) {
        Icon(
            painter = painterResource(R.drawable.ic_mongging_feed),
            contentDescription = "먹이",
            tint = Color.Unspecified,
            modifier = Modifier.size(80.dp)
        )
    }
}