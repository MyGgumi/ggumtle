package com.ggumtle.mission.component

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Paint
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.res.painterResource
import android.graphics.BlurMaskFilter
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.AppGradients
import com.ggumtle.designsystem.theme.BrandColors
import com.ggumtle.mission.model.BeforeSuccessMission

@Composable
fun RemainMissionDialog(
    beforeSuccessMissions: List<BeforeSuccessMission>,
    onDismiss: () -> Unit,
    modifier: Modifier = Modifier
) {
    AnimatedVisibility(
        visible = true,
        enter = fadeIn() + scaleIn(),
        exit = fadeOut() + scaleOut(),
        modifier = modifier
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .clickable { onDismiss() },
            contentAlignment = Alignment.Center
        ) {
            Box {
                Box(
                    modifier = Modifier
                        .width(320.dp)
                        .drawBehind {
                            // 보라색 후광 효과
                            drawIntoCanvas { canvas ->
                                val paint = Paint().apply {
                                    color = BrandColors.PurpleLight.copy(alpha = 0.6f)
                                }
                                paint.asFrameworkPaint().apply {
                                    maskFilter = BlurMaskFilter(60f, BlurMaskFilter.Blur.NORMAL)
                                }
                                canvas.nativeCanvas.drawRoundRect(
                                    0f, 0f, size.width, size.height,
                                    60f, 60f, paint.asFrameworkPaint()
                                )
                            }
                        }
                        .clip(RoundedCornerShape(20.dp))
                        .background(Color(0xFF2A1B4A))
                        .border(
                            width = 1.dp,
                            color = BrandColors.PurpleLight.copy(alpha = 0.3f),
                            shape = RoundedCornerShape(20.dp)
                        )
                        .clickable { /* 내부 클릭 시 닫히지 않음 */ }
                ) {
                    Column(
                        modifier = Modifier.padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        // 제목
                        Text(
                            text = "일일 미션",
                            color = Color.White,
                            style = MaterialTheme.typography.headlineMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(8.dp))

                        // 완료 현황
                        Text(
                            text = "미완료 미션 ${beforeSuccessMissions.size}개",
                            color = BrandColors.PurpleLight,
                            style = MaterialTheme.typography.bodyMedium
                        )


                        Spacer(modifier = Modifier.height(24.dp))

                        // 미완료 미션 목록
                        beforeSuccessMissions.forEach { mission ->
                            AnimatedVisibility(
                                visible = true,
                                enter = slideInVertically(
                                    animationSpec = tween(400, easing = EaseOutCubic)
                                ) + fadeIn(animationSpec = tween(400)),
                                exit = slideOutVertically(
                                    animationSpec = tween(400, easing = EaseInCubic)
                                ) + fadeOut(animationSpec = tween(400))
                            ) {
                                Column {
                                    BeforeSuccessMissionItem(
                                        mission = mission
                                    )
                                    Spacer(modifier = Modifier.height(12.dp))
                                }
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))

                        // 하단 안내문
                        Text(
                            text = "~매일 밤 12시 초기화~",
                            color = BrandColors.PurpleLight.copy(alpha = 0.7f),
                            style = MaterialTheme.typography.bodySmall,
                            textAlign = TextAlign.Center
                        )
                    }
                }

                // 닫기 버튼
                Box(
                    modifier = Modifier
                        .align(Alignment.TopEnd)
                        .offset(x = 10.dp, y = (-10).dp)
                        .size(32.dp)
                        .background(
                            BrandColors.PurpleDark,
                            CircleShape
                        )
                        .border(
                            width = 1.dp,
                            color = BrandColors.PurpleLight.copy(alpha = 0.5f),
                            shape = CircleShape
                        )
                        .clickable { onDismiss() },
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "✕",
                        color = Color.White,
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }
}

@Composable
private fun BeforeSuccessMissionItem(
    mission: BeforeSuccessMission,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .fillMaxWidth()
            .background(
                Color(0xFF1F1533),
                RoundedCornerShape(12.dp)
            )
            .border(
                width = 1.dp,
                color = BrandColors.PurpleLight.copy(alpha = 0.5f),
                shape = RoundedCornerShape(12.dp)
            )
            .padding(16.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = mission.missionName,
                    color = Color.White,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium
                )

                Spacer(modifier = Modifier.height(6.dp))

                // 미완료 상태 표시
                Box(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(4.dp)
                        .background(
                            BrandColors.PurpleDark,
                            RoundedCornerShape(2.dp)
                        )
                )
            }

            Spacer(modifier = Modifier.width(12.dp))

            // 남은 개수 표시
            Text(
                text = "${mission.remainCount} 회",
                color = BrandColors.PurpleLight,
                style = MaterialTheme.typography.bodyMedium,
                fontWeight = FontWeight.Bold
            )
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF2D1B69)
@Composable
fun DailyMissionDialogPreview() {
    val sampleBeforeSuccessMissions = listOf(
        BeforeSuccessMission(
            missionId = 1L,
            memberMissionId = 1L,
            missionName = "몽깅이와 사진찍기",
            remainCount = 1
        ),
        BeforeSuccessMission(
            missionId = 2L,
            memberMissionId = 2L,
            missionName = "몽깅이 쓰다듬기",
            remainCount = 2
        ),
        BeforeSuccessMission(
            missionId = 3L,
            memberMissionId = 3L,
            missionName = "친구에게 '좋은 꿈' 보내기",
            remainCount = 1
        )
    )

    RemainMissionDialog(
        beforeSuccessMissions = sampleBeforeSuccessMissions,
        onDismiss = {}
    )
}