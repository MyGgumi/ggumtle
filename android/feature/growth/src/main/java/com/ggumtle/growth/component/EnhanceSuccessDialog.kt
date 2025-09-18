package com.ggumtle.growth.component

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.scaleIn
import androidx.compose.animation.scaleOut
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.graphics.Paint
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.graphics.nativeCanvas
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.BrandColors
import android.graphics.BlurMaskFilter
import androidx.compose.foundation.border

@Composable
fun EnhanceSuccessDialog(
    isVisible: Boolean,
    nextSuccessRate: Int,
    currentLevel: Int = 4,
    nextLevel: Int = 5,
    currentStat: Float = 1.2f,
    nextStat: Float = 1.25f,
    statisticName: String = "치료속도",
    onDismiss: () -> Unit,
    modifier: Modifier = Modifier
) {
    AnimatedVisibility(
        visible = isVisible,
        enter = fadeIn() + scaleIn(),
        exit = fadeOut() + scaleOut(),
        modifier = modifier
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Color.Transparent)
                .clickable { onDismiss() },
            contentAlignment = Alignment.Center
        ) {
            Box(
                modifier = Modifier
                    .width(300.dp)
                    .height(280.dp)
                    .drawBehind {
                        // 민트색 후광 효과 - 더 크고 밝게
                        drawIntoCanvas { canvas ->
                            val paint = Paint().apply {
                                color = BrandColors.Mint.copy(alpha = 0.7f)
                            }
                            paint.asFrameworkPaint().apply {
                                maskFilter = BlurMaskFilter(80f, BlurMaskFilter.Blur.NORMAL)
                            }
                            canvas.nativeCanvas.drawRoundRect(
                                0f, 0f, size.width, size.height,
                                60f, 60f, paint.asFrameworkPaint()
                            )
                        }
                    }
                    .clip(RoundedCornerShape(20.dp))
                    .background(Color(0xFF2A3441))
                    .border(
                        width = 2.dp,
                        color = BrandColors.Mint.copy(alpha = 0.8f),
                        shape = RoundedCornerShape(20.dp)
                    )
                    .clickable { /* 내부 클릭 시 닫히지 않음 */ },
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally
                ) {
                    Image(
                    painter = painterResource(id = R.drawable.ic_success_star),
                        contentDescription = "Success",
                        modifier = Modifier.size(40.dp)
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    Text(
                        text = "강화 성공!",
                        color = Color.White,
                        style = MaterialTheme.typography.headlineLarge,
                        textAlign = TextAlign.Center
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    Text(
                        text = "다음 성공확률 ${nextSuccessRate}%",
                        color = BrandColors.PurpleLight,
                        style = MaterialTheme.typography.bodyMedium,
                        textAlign = TextAlign.Center
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Text(
                            text = "레벨",
                            color = BrandColors.PurpleLight,
                            style = MaterialTheme.typography.bodyMedium
                        )

                        Text(
                            text = "Lv $currentLevel",
                            color = Color.White,
                            style = MaterialTheme.typography.bodyMedium
                        )

                        Text(
                            text = "→",
                            color = BrandColors.Mint,
                            style = MaterialTheme.typography.bodyLarge
                        )

                        Text(
                            text = "Lv $nextLevel",
                            color = BrandColors.Mint,
                            style = MaterialTheme.typography.titleLarge
                        )
                    }

                    Spacer(modifier = Modifier.height(8.dp))

                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {

                        Text(
                            text = statisticName,
                            color = BrandColors.PurpleLight,
                            style = MaterialTheme.typography.bodyMedium
                        )

                        Text(
                            text = "${currentStat}x",
                            color = Color.White,
                            style = MaterialTheme.typography.bodyMedium
                        )

                        Text(
                            text = "→",
                            color = BrandColors.Mint,
                            style = MaterialTheme.typography.bodyLarge
                        )

                        Text(
                            text = "${nextStat}x",
                            color = BrandColors.Mint,
                            style = MaterialTheme.typography.titleLarge
                        )
                    }
                }
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF2D1B69)
@Composable
fun EnhanceSuccessDialogPreview() {
    EnhanceSuccessDialog(
        isVisible = true,
        nextSuccessRate = 10,
        currentLevel = 4,
        nextLevel = 5,
        currentStat = 1.2f,
        nextStat = 1.25f,
        onDismiss = {}
    )
}