package com.ggumtle.mission.component

import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.spring
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
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
    // 빛나는 효과를 위한 무한 애니메이션
    val infiniteTransition = rememberInfiniteTransition(label = "glow_animation")
    val glowAlpha by infiniteTransition.animateFloat(
        initialValue = 0.7f,
        targetValue = 1.0f,
        animationSpec = infiniteRepeatable(
            animation = tween(1500),
            repeatMode = androidx.compose.animation.core.RepeatMode.Reverse
        ),
        label = "glow_alpha"
    )

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
            .size(60.dp)
            .scale(scale)
            .shadow(
                elevation = 8.dp,
                shape = CircleShape,
                ambientColor = Color(0xFF9C27B0),
                spotColor = Color(0xFF9C27B0)
            )
            .background(
                brush = Brush.radialGradient(
                    colors = listOf(
                        Color(0xFFE1BEE7).copy(alpha = glowAlpha), // 연한 보라색 중심
                        Color(0xFF9C27B0).copy(alpha = glowAlpha), // 진한 보라색 외곽
                        Color(0xFF673AB7).copy(alpha = glowAlpha * 0.8f) // 더 진한 보라색 테두리
                    ),
                    radius = 100f
                ),
                shape = CircleShape
            )
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
    )
}