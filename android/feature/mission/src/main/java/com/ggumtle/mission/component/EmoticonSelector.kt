package com.ggumtle.mission.component

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.theme.BrandColors
import com.ggumtle.mission.model.EmoticonData
import com.ggumtle.mission.model.EmoticonItem

@Composable
fun EmoticonSelector(
    onEmoticonSelected: (Int) -> Unit,
    modifier: Modifier = Modifier
) {
    AnimatedVisibility(
        visible = true,
        enter = slideInHorizontally(
            initialOffsetX = { it },
            animationSpec = spring(
                dampingRatio = Spring.DampingRatioLowBouncy,
                stiffness = Spring.StiffnessLow
            )
        ) + fadeIn(),
        exit = slideOutHorizontally(
            targetOffsetX = { it },
            animationSpec = tween(300)
        ) + fadeOut(),
        modifier = modifier
    ) {
        Row(
            modifier = Modifier
                .background(
                    Color.Black.copy(alpha = 0.7f),
                    RoundedCornerShape(25.dp)
                )
                .border(
                    width = 1.dp,
                    color = BrandColors.PurpleLight.copy(alpha = 0.5f),
                    shape = RoundedCornerShape(25.dp)
                )
                .padding(horizontal = 6.dp, vertical = 4.dp),
            horizontalArrangement = Arrangement.spacedBy(4.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            EmoticonData.emoticons.forEachIndexed { index, emoticon ->
                key(emoticon.animationIndex) {
                    EmoticonButton(
                        emoticon = emoticon,
                        onEmoticonSelected = onEmoticonSelected
                    )
                }

                // 마지막 아이템이 아닐 때만 구분선
                if (index < EmoticonData.emoticons.size - 1) {
                    Box(
                        modifier = Modifier
                            .width(1.dp)
                            .height(40.dp)
                            .background(BrandColors.PurpleLight.copy(alpha = 0.2f))
                    )
                }
            }
        }
    }
}

@Composable
private fun EmoticonButton(
    emoticon: EmoticonItem,
    onEmoticonSelected: (Int) -> Unit,
    modifier: Modifier = Modifier
) {
    var isPressed by remember { mutableStateOf(false) }

    val scale by animateFloatAsState(
        targetValue = if (isPressed) 0.9f else 1f,
        animationSpec = spring(
            dampingRatio = Spring.DampingRatioMediumBouncy,
            stiffness = Spring.StiffnessHigh
        ),
        label = "emoticonScale",
        finishedListener = {
            if (isPressed) {
                isPressed = false
            }
        }
    )

    Box(
        modifier = modifier
            .size(40.dp)
            .scale(scale)
            .clickable {
                isPressed = true
                onEmoticonSelected(emoticon.animationIndex)
            },
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = emoticon.emoji,
            fontSize = 20.sp
        )
    }
}