package com.ggumtle.designsystem.component

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.geometry.CornerRadius
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.*
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import android.graphics.BlurMaskFilter
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.AppGradients
import com.ggumtle.designsystem.theme.BrandColors
import java.text.DecimalFormat

@Composable
fun DreamCoinDisplay(
    dreamCoin: Int,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .drawBehind {
                val cornerRadius = 24.dp.toPx()

                // 여러 겹의 후광 효과
                drawIntoCanvas { canvas ->
                    val paint = Paint().apply {
                        color = BrandColors.GradientStart
                        isAntiAlias = true
                    }

                    // 가장 바깥쪽 후광 (큰 blur)
                    paint.asFrameworkPaint().apply {
                        maskFilter = BlurMaskFilter(20f, BlurMaskFilter.Blur.NORMAL)
                        color = BrandColors.GradientStart.copy(alpha = 0.3f).toArgb()
                    }
                    canvas.drawRoundRect(
                        left = -10f,
                        top = -10f,
                        right = size.width + 10f,
                        bottom = size.height + 10f,
                        radiusX = cornerRadius + 10f,
                        radiusY = cornerRadius + 10f,
                        paint = paint
                    )

                    // 중간 후광
                    paint.asFrameworkPaint().apply {
                        maskFilter = BlurMaskFilter(12f, BlurMaskFilter.Blur.NORMAL)
                        color = BrandColors.GradientEnd.copy(alpha = 0.4f).toArgb()
                    }
                    canvas.drawRoundRect(
                        left = -5f,
                        top = -5f,
                        right = size.width + 5f,
                        bottom = size.height + 5f,
                        radiusX = cornerRadius + 5f,
                        radiusY = cornerRadius + 5f,
                        paint = paint
                    )

                    // 안쪽 후광 (밝은 효과)
                    paint.asFrameworkPaint().apply {
                        maskFilter = BlurMaskFilter(6f, BlurMaskFilter.Blur.NORMAL)
                        color = Color.White.copy(alpha = 0.2f).toArgb()
                    }
                    canvas.drawRoundRect(
                        left = 0f,
                        top = 0f,
                        right = size.width,
                        bottom = size.height,
                        radiusX = cornerRadius,
                        radiusY = cornerRadius,
                        paint = paint
                    )
                }
            },
        contentAlignment = Alignment.Center
    ) {
        // 메인 버튼
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clip(RoundedCornerShape(24.dp))
                .background(AppGradients.Primary)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(horizontal = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Image(
                    painter = painterResource(id = R.drawable.ic_moon),
                    contentDescription = "Dream Coin",
                    modifier = Modifier.size(24.dp)
                )
                Text(
                    text = DecimalFormat("#,###").format(dreamCoin),
                    color = Color.White,
                    style = MaterialTheme.typography.bodySmall
                )
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun DreamCoinDisplayPreview() {
    DreamCoinDisplay(dreamCoin = 9999)
}