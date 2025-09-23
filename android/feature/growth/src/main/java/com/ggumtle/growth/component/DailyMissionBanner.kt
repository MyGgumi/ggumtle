package com.ggumtle.growth.component

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.component.PurpleBlackBox
import com.ggumtle.designsystem.theme.BrandColors
import com.ggumtle.growth.model.DailyMission

@Composable
fun DailyMissionBanner(
    dailyMission: DailyMission?,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    PurpleBlackBox(
        modifier = modifier
            .clickable { onClick() },
        contentPadding = PaddingValues(20.dp),
        contentAlignment = Alignment.Center
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp),
                contentAlignment = Alignment.Center
            ) {
                Image(
                    painter = painterResource(id = R.drawable.ic_mission),
                    contentDescription = "Mission",
                    modifier = Modifier.size(32.dp)
                )
            }
            
            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = "일일 미션",
                    color = Color.White,
                    style = MaterialTheme.typography.bodyLarge
                )
                Text(
                    text = dailyMission?.let {
                        "완료까지 ${it.remainingMissionsCount}개"
                    } ?: "미션 정보 없음",
                    color = BrandColors.PurpleLight,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
            
            // 진행도 인디케이터 (선으로 연결된 원들)
            Box(
                modifier = Modifier
                    .width(100.dp)
                    .height(16.dp),
                contentAlignment = Alignment.Center
            ) {
                val totalMissions = dailyMission?.totalMissionsCount ?: 5
                val completedMissions = dailyMission?.completedMissionsCount ?: 0

                // 배경 선
                Canvas(
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(2.dp)
                ) {
                    drawLine(
                        color = BrandColors.PurpleLight.copy(alpha = 0.3f),
                        start = androidx.compose.ui.geometry.Offset(0f, size.height / 2),
                        end = androidx.compose.ui.geometry.Offset(size.width, size.height / 2),
                        strokeWidth = 2.dp.toPx()
                    )
                }

                // 완료된 부분의 선
                if (completedMissions > 1) {
                    Canvas(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(2.dp)
                    ) {
                        val progressWidth = size.width * (completedMissions - 1) / (totalMissions - 1).toFloat()
                        drawLine(
                            color = BrandColors.Mint,
                            start = androidx.compose.ui.geometry.Offset(0f, size.height / 2),
                            end = androidx.compose.ui.geometry.Offset(progressWidth, size.height / 2),
                            strokeWidth = 2.dp.toPx()
                        )
                    }
                }

                // 원들
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    repeat(totalMissions) { index ->
                        val isCompleted = index < completedMissions

                        Box(
                            modifier = Modifier
                                .size(12.dp)
                                .background(
                                    color = if (isCompleted) BrandColors.Mint else BrandColors.PurpleLight,
                                    shape = CircleShape
                                )
                                .border(
                                    width = 1.dp,
                                    color = Color.White.copy(alpha = 0.3f),
                                    shape = CircleShape
                                ),
                            contentAlignment = Alignment.Center
                        ) {
                            if (isCompleted) {
                                Text(
                                    text = "✓",
                                    color = Color.White,
                                    fontSize = 8.sp,
                                    fontWeight = FontWeight.Bold
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun DailyMissionBannerPreview() {
    DailyMissionBanner(
        dailyMission = DailyMission(
            totalMissionsCount = 5,
            completedMissionsCount = 3,
            remainingMissionsCount = 2,
            afterRewordMissionsCount = 0
        ),
        onClick = {}
    )
}