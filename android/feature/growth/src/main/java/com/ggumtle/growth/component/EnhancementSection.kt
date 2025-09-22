package com.ggumtle.growth.component

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.animation.core.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.LinearGradientShader
import androidx.compose.ui.graphics.Shader
import androidx.compose.ui.graphics.ShaderBrush
import androidx.compose.ui.graphics.TileMode
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.graphics.Paint
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.graphics.nativeCanvas
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.component.PurpleBlackBox
import com.ggumtle.designsystem.theme.AppGradients
import com.ggumtle.designsystem.theme.BrandColors
import com.ggumtle.growth.model.CharacterInfo
import java.text.DecimalFormat
import android.graphics.BlurMaskFilter
import androidx.compose.foundation.border
import com.ggumtle.domain.model.MonggingClass

@Composable
fun EnhancementSection(
    characterInfo: CharacterInfo?,
    currentCoin: Int,
    isEnhanceEnabled: Boolean,
    onEnhanceClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val maxCost = characterInfo?.needCoin ?: 4000
    val targetProgressRatio = if (currentCoin >= maxCost) 1f else currentCoin.toFloat() / maxCost
    val progressRatio by animateFloatAsState(
        targetValue = targetProgressRatio,
        animationSpec = tween(
            durationMillis = 800,
            easing = EaseOutCubic
        ),
        label = "progressAnimation"
    )


    // 흰빛 흐르는 애니메이션
    val infiniteTransition = rememberInfiniteTransition(label = "shimmerTransition")
    val shimmerOffset by infiniteTransition.animateFloat(
        initialValue = -1.5f,
        targetValue = 1.5f,
        animationSpec = infiniteRepeatable(
            animation = tween(2000, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "shimmerAnimation"
    )

    val formatter = remember { DecimalFormat("#,###") }

    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        PurpleBlackBox(
            modifier = Modifier.fillMaxWidth(),
            contentPadding = PaddingValues(30.dp),
            contentAlignment = Alignment.Center
        ) {
            Column(
                modifier = Modifier.fillMaxWidth(),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                characterInfo?.let { info ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "Lv ${info.nowLevel}",
                                color = Color.White,
                                style = MaterialTheme.typography.titleLarge
                            )
                            Text(
                                text = "경험치 ${String.format("%.1f", info.nowPercentage)}%",
                                color = BrandColors.PurpleLight,
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }

                        Image(
                            painter = painterResource(id = R.drawable.ic_right_arrows),
                            contentDescription = "Arrow",
                            modifier = Modifier.size(32.dp)
                        )

                        Column(
                            horizontalAlignment = Alignment.End
                        ) {
                            Text(
                                text = "Lv ${info.afterLevel ?: (info.nowLevel + 1)}",
                                color = BrandColors.Mint,
                                style = MaterialTheme.typography.titleLarge
                            )
                            Text(
                                text = "경험치 ${String.format("%.1f", info.afterPercentage ?: 0.0)}%",
                                color = BrandColors.Mint,
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
                    }

                    Spacer(modifier = Modifier.height(16.dp))
                }

                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(12.dp)
                ) {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(12.dp)
                            .background(
                                Color(0xFF2D1F43),
                                RoundedCornerShape(20.dp)
                            )
                    )
                    
                    Box(
                        modifier = Modifier
                            .fillMaxWidth(progressRatio)
                            .height(12.dp)
                            .background(
                                AppGradients.Primary,
                                RoundedCornerShape(16.dp)
                            )
                    )

                }

                Spacer(modifier = Modifier.height(4.dp))

                Box(
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        text = "${formatter.format(currentCoin)} / ${formatter.format(maxCost)}",
                        color = BrandColors.PurpleLight,
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.align(Alignment.CenterEnd)
                    )
                }

                Spacer(modifier = Modifier.height(16.dp))

                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(80.dp)
                        .let { modifier ->
                            if (progressRatio >= 1f) {
                                modifier.drawBehind {
                                    drawIntoCanvas { canvas ->
                                        val paint = Paint().apply {
                                            color = BrandColors.GradientEnd.copy(alpha = 0.6f)
                                        }
                                        paint.asFrameworkPaint().apply {
                                            maskFilter = BlurMaskFilter(30f, BlurMaskFilter.Blur.NORMAL)
                                        }
                                        canvas.nativeCanvas.drawRoundRect(
                                            0f, 0f, size.width, size.height,
                                            48f, 48f, paint.asFrameworkPaint()
                                        )
                                    }
                                }
                            } else modifier
                        }
                        .background(
                            brush = if (progressRatio >= 1f) AppGradients.Primary else Brush.horizontalGradient(
                                colors = listOf(BrandColors.PurpleDark, BrandColors.PurpleDark)
                            ),
                            shape = RoundedCornerShape(16.dp)
                        )
                        .border(
                            width = 1.dp,
                            brush = Brush.horizontalGradient(
                                colors = listOf(
                                    Color.White.copy(alpha = 0.4f),
                                    Color.White.copy(alpha = 0.1f)
                                )
                            ),
                            shape = RoundedCornerShape(16.dp)
                        )
                        .clickable(enabled = isEnhanceEnabled || progressRatio >= 1f) {
                            onEnhanceClick()
                        },
                    contentAlignment = Alignment.Center
                ) {
                    // 흰빛 애니메이션 레이어 (활성화 시에만)
                    if (progressRatio >= 1f) {
                        Box(
                            modifier = Modifier
                                .fillMaxSize()
                                .background(
                                    brush = Brush.linearGradient(
                                        colors = listOf(
                                            Color.Transparent,
                                            Color.White.copy(alpha = 0.1f),
                                            Color.White.copy(alpha = 0.4f),
                                            Color.White.copy(alpha = 0.1f),
                                            Color.Transparent
                                        ),
                                        start = Offset(shimmerOffset * 800f - 150f, 0f),
                                        end = Offset(shimmerOffset * 800f + 150f, 0f)
                                    ),
                                    shape = RoundedCornerShape(16.dp)
                                )
                        )
                    }

                    Column(
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(
                            text = "강화하기",
                            style = MaterialTheme.typography.titleLarge,
                            color = Color.White
                        )
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(4.dp)
                        ) {
                            Image(
                                painter = painterResource(id = R.drawable.ic_moon),
                                contentDescription = "Dream Coin",
                                modifier = Modifier.size(24.dp)
                            )
                            Text(
                                text = "- ${formatter.format(maxCost)}    |    성공 확률 ${characterInfo?.successPercentage ?: 35}%",
                                style = MaterialTheme.typography.bodyMedium,
                                color = Color.White.copy(alpha = 0.8f)
                            )
                        }
                    }
                }

            }
        }
        
        
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun EnhancementSectionPreview() {
    EnhancementSection(
        characterInfo = CharacterInfo(
            id = 1L,
            monggingClass = MonggingClass.HEAL,
            nowLevel = 4,
            nowPercentage = 30.0,
            isMaxLevel = false,
            afterLevel = 5,
            afterPercentage = 0.0,
            needCoin = 4000,
            successPercentage = 35
        ),
        currentCoin = 3000,
        isEnhanceEnabled = false,
        onEnhanceClick = {},
    )
}