package com.ggumtle.growth.component

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
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.drawBehind
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Paint
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.drawscope.drawIntoCanvas
import androidx.compose.ui.unit.offset
import androidx.compose.ui.graphics.nativeCanvas
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.zIndex
import android.graphics.BlurMaskFilter
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.AppGradients
import com.ggumtle.designsystem.theme.BrandColors
import com.ggumtle.growth.model.Mission

@Composable
fun DailyMissionDialog(
    isVisible: Boolean,
    missions: List<Mission>,
    completedCount: Int,
    totalCount: Int,
    onDismiss: () -> Unit,
    onClaimReward: (Mission) -> Unit,
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
                .background(Color.Black.copy(alpha = 0.8f))
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
                        text = "완료까지 ${totalCount - completedCount}개 남음 ($completedCount/$totalCount)",
                        color = BrandColors.PurpleLight,
                        style = MaterialTheme.typography.bodyMedium
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // 진행도 인디케이터 (선으로 연결된 원들)
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(24.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        // 배경 선
                        Canvas(
                            modifier = Modifier
                                .fillMaxWidth(0.8f)
                                .height(2.dp)
                        ) {
                            drawLine(
                                color = BrandColors.PurpleLight.copy(alpha = 0.3f),
                                start = androidx.compose.ui.geometry.Offset(0f, size.height / 2),
                                end = androidx.compose.ui.geometry.Offset(size.width, size.height / 2),
                                strokeWidth = 4.dp.toPx()
                            )
                        }

                        // 완료된 부분의 선 (애니메이션)
                        if (completedCount > 1) {
                            val animatedProgress by animateFloatAsState(
                                targetValue = (completedCount - 1) / (totalCount - 1).toFloat(),
                                animationSpec = tween(
                                    durationMillis = 800,
                                    easing = EaseOutCubic
                                ),
                                label = "progressLineAnimation"
                            )

                            Canvas(
                                modifier = Modifier
                                    .fillMaxWidth(0.8f)
                                    .height(2.dp)
                            ) {
                                val progressWidth = size.width * animatedProgress
                                drawLine(
                                    color = BrandColors.Mint,
                                    start = androidx.compose.ui.geometry.Offset(0f, size.height / 2),
                                    end = androidx.compose.ui.geometry.Offset(progressWidth, size.height / 2),
                                    strokeWidth = 4.dp.toPx()
                                )
                            }
                        }

                        // 원들
                        Row(
                            modifier = Modifier.fillMaxWidth(0.8f),
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            repeat(totalCount) { index ->
                                val isCompleted = index < completedCount

                                // 원형 인디케이터 애니메이션
                                val animatedColor by animateColorAsState(
                                    targetValue = if (isCompleted) BrandColors.Mint else BrandColors.PurpleLight,
                                    animationSpec = tween(
                                        durationMillis = 600,
                                        delayMillis = index * 100
                                    ),
                                    label = "circleColorAnimation"
                                )

                                val animatedScale by animateFloatAsState(
                                    targetValue = if (isCompleted) 1.2f else 1f,
                                    animationSpec = spring(
                                        dampingRatio = Spring.DampingRatioMediumBouncy,
                                        stiffness = Spring.StiffnessLow
                                    ),
                                    label = "circleScaleAnimation"
                                )

                                Box(
                                    modifier = Modifier
                                        .size(16.dp)
                                        .scale(animatedScale)
                                        .background(
                                            color = animatedColor,
                                            shape = CircleShape
                                        )
                                        .border(
                                            width = 2.dp,
                                            color = Color.White.copy(alpha = 0.3f),
                                            shape = CircleShape
                                        ),
                                    contentAlignment = Alignment.Center
                                ) {
                                    if (isCompleted) {
                                        Text(
                                            text = "✓",
                                            color = Color.White,
                                            fontSize = 10.sp,
                                            fontWeight = FontWeight.Bold
                                        )
                                    }
                                }
                            }
                        }
                    }

                    Spacer(modifier = Modifier.height(24.dp))

                    // 미션 목록 (정렬: 보상받기 -> 미완료 -> 완료됨)
                    val sortedMissions = missions.sortedWith { mission1, mission2 ->
                        when {
                            // 보상받기 가능한 미션이 최상단
                            mission1.isCompleted && !mission1.isRewarded && !(mission2.isCompleted && !mission2.isRewarded) -> -1
                            mission2.isCompleted && !mission2.isRewarded && !(mission1.isCompleted && !mission1.isRewarded) -> 1
                            // 미완료 미션이 중간
                            !mission1.isCompleted && mission2.isCompleted && mission2.isRewarded -> -1
                            !mission2.isCompleted && mission1.isCompleted && mission1.isRewarded -> 1
                            else -> 0
                        }
                    }

                    sortedMissions.forEach { mission ->
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
                                MissionItem(
                                    mission = mission,
                                    onClaimReward = { onClaimReward(mission) }
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
private fun MissionItem(
    mission: Mission,
    onClaimReward: () -> Unit,
    modifier: Modifier = Modifier
) {
    // 말풍선을 박스 밖에 배치하기 위한 상위 컨테이너
    Box(
        modifier = modifier.fillMaxWidth()
    ) {
        // 미션 박스
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .background(
                    Color(0xFF1F1533),
                    RoundedCornerShape(12.dp)
                )
                .border(
                    width = 1.dp,
                    color = if (mission.isCompleted && mission.isRewarded) Color.Transparent else BrandColors.PurpleLight.copy(alpha = 0.5f),
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
                    text = mission.title,
                    color = if (mission.isCompleted && mission.isRewarded) Color.White.copy(alpha = 0.3f) else Color.White,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.Medium,
                    textDecoration = if (mission.isCompleted && mission.isRewarded) TextDecoration.LineThrough else TextDecoration.None
                )

                Spacer(modifier = Modifier.height(6.dp))

                if (mission.isCompleted && mission.isRewarded) {
                    // 보상받기 완료 - PurpleLight
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(4.dp)
                            .background(
                                BrandColors.PurpleLight.copy(alpha = 0.3f),
                                RoundedCornerShape(2.dp)
                            )
                    )
                } else if (mission.isCompleted) {
                    // 완료했지만 보상 안받음 - 그라데이션
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(4.dp)
                            .background(
                                brush = AppGradients.Primary,
                                shape = RoundedCornerShape(2.dp)
                            )
                    )
                } else {
                    // 미완료 - PurpleDark
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
            }

            Spacer(modifier = Modifier.width(12.dp))

            // 보상/상태
            if (mission.isCompleted && !mission.isRewarded) {
                var isClicked by remember { mutableStateOf(false) }

                // 받기 버튼
                Box(
                    modifier = Modifier
                        .background(
                            AppGradients.Primary,
                            RoundedCornerShape(8.dp)
                        )
                        .clickable {
                            isClicked = true
                            onClaimReward()
                        }
                        .padding(horizontal = 16.dp, vertical = 8.dp)
                ) {
                    Text(
                        text = "받기",
                        color = Color.White,
                        style = MaterialTheme.typography.bodySmall,
                        fontWeight = FontWeight.Bold
                    )
                }

            } else if (mission.isCompleted && mission.isRewarded) {
                // 완료 체크
                Box(
                    modifier = Modifier
                        .background(
                            BrandColors.PurpleDark.copy(alpha = 0.3f),
                            RoundedCornerShape(8.dp)
                        )
                        .padding(horizontal = 16.dp, vertical = 8.dp)
                ) {
                    Text(
                        text = "✓ 완료",
                        color = BrandColors.PurpleLight.copy(alpha = 0.3f),
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Bold
                    )
                }
            } else {
                // 보상 표시
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(4.dp)
                ) {

                    Image(
                        painter = painterResource(id = R.drawable.ic_dream_coin),
                        contentDescription = "Dream Coin",
                        modifier = Modifier.size(24.dp)
                    )
                    Text(
                        text = "+${mission.reward}",
                        color = BrandColors.Mint,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
        }
    }

        // 말풍선을 박스 밖에 별도로 배치
        if (mission.isCompleted && !mission.isRewarded) {
            Column(
                modifier = Modifier
                    .align(Alignment.CenterEnd)
                    .padding(end = 8.dp)
                    .offset(y = (-32).dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                // 툴팁 (말풍선)
                Box(
                    modifier = Modifier
                        .background(
                            BrandColors.PurpleLight.copy(alpha = 0.5f),
                            RoundedCornerShape(6.dp)
                        )
                        .padding(horizontal = 8.dp, vertical = 4.dp)
                ) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.spacedBy(4.dp)
                    ) {
                        Image(
                            painter = painterResource(id = R.drawable.ic_dream_coin),
                            contentDescription = "Coin",
                            modifier = Modifier.size(18.dp)
                        )
                        Text(
                            text = "+${mission.reward}",
                            color = BrandColors.Mint,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }

                // 말풍선 꼬리 (삼각형)
                Box(
                    modifier = Modifier
                        .size(width = 10.dp, height = 5.dp)
                        .offset(y = (-0).dp)
                ) {
                    Canvas(
                        modifier = Modifier.fillMaxSize()
                    ) {
                        val path = Path()
                        path.moveTo(size.width / 2, size.height)
                        path.lineTo(0f, 0f)
                        path.lineTo(size.width, 0f)
                        path.close()
                        drawPath(
                            path = path,
                            color = BrandColors.PurpleLight.copy(alpha = 0.5f)
                        )
                    }
                }
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF2D1B69)
@Composable
fun DailyMissionDialogPreview() {
    val sampleMissions = listOf(
        Mission(
            id = "1",
            title = "몽깅이와 사진찍기",
            reward = 100,
            isCompleted = true,
            isRewarded = false
        ),
        Mission(
            id = "2",
            title = "악몽 1회 탈출",
            reward = 100,
            isCompleted = true,
            isRewarded = true
        ),
        Mission(
            id = "3",
            title = "친구에게 '좋은 꿈' 보내기",
            reward = 50,
            isCompleted = false,
            isRewarded = false
        ),
        Mission(
            id = "4",
            title = "몽깅이 쓰다듬기",
            reward = 50,
            isCompleted = true,
            isRewarded = true
        ),
        Mission(
            id = "5",
            title = "몽깅이 밥주기",
            reward = 75,
            isCompleted = false,
            isRewarded = false
        )
    )

    DailyMissionDialog(
        isVisible = true,
        missions = sampleMissions,
        completedCount = 3,
        totalCount = 5,
        onDismiss = {},
        onClaimReward = {}
    )
}