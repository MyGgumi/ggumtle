package com.ggumtle.home.component

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.component.button.GameIconButton
import com.ggumtle.designsystem.theme.GameColors
import com.example.domain.websocket.model.PartyMember
import kotlin.invoke

@Composable
fun PartySection(
    partyMembers: List<PartyMember>,
    isPartyLeader: Boolean,
    canStartGame: Boolean,
    isLoading: Boolean,
    onInviteFriendsClick: () -> Unit,
    onToggleReady: () -> Unit,
    onStartGame: () -> Unit,
    onCancelGameSearch: () -> Unit,
    isReady: Boolean,
    isSearchingGame: Boolean,
    matchmakingTimeSeconds: Int,
    modifier: Modifier = Modifier
) {
    var isPartyExpanded by remember { mutableStateOf(false) }

    Box(
        modifier = modifier.fillMaxWidth()
    ) {
        // 게임 시작/레디 버튼 (하단 중앙)
        Box(
            modifier = Modifier
                .align(Alignment.BottomCenter)
                .padding(bottom = 16.dp)
        ) {
            if (isSearchingGame) {
                // 게임 찾는 중일 때 - 리더/멤버 상관없이 모든 사람에게 표시
                Box {
                    // 게임 찾는 중 버튼 (비활성화) - 센터 고정
                    Button(
                        onClick = { }, // 클릭 불가
                        enabled = false,
                        colors = ButtonDefaults.buttonColors(
                            disabledContainerColor = GameColors.textSecondary
                        ),
                        shape = RoundedCornerShape(16.dp),
                        modifier = Modifier
                            .align(Alignment.Center)
                            .padding(horizontal = 32.dp)
                    ) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(16.dp),
                            color = Color.White,
                            strokeWidth = 2.dp
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                "게임 찾는 중...",
                                fontWeight = FontWeight.Bold,
                                fontSize = 14.sp,
                                color = Color.White
                            )
                            Text(
                                formatTime(matchmakingTimeSeconds),
                                fontWeight = FontWeight.Medium,
                                fontSize = 12.sp,
                                color = Color.White.copy(alpha = 0.8f)
                            )
                        }
                    }
                    
                    // X 취소 버튼 (버튼 오른쪽에 4dp 간격)
                    Box(
                        modifier = Modifier
                            .size(24.dp)
                            .background(
                                color = Color.Gray.copy(alpha = 0.3f),
                                shape = CircleShape
                            )
                            .clickable { onCancelGameSearch() }
                            .align(Alignment.CenterEnd),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(
                            imageVector = Icons.Default.Close,
                            contentDescription = "취소",
                            tint = Color.Gray,
                            modifier = Modifier.size(16.dp)
                        )
                    }
                }
            } else if (isPartyLeader) {
                Button(
                    onClick = onStartGame,
                    enabled = canStartGame && !isLoading,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = if (canStartGame) GameColors.primary else GameColors.textSecondary
                    ),
                    shape = RoundedCornerShape(16.dp),
                    modifier = Modifier.padding(horizontal = 32.dp)
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(20.dp),
                            color = Color.White,
                            strokeWidth = 2.dp
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                    }
                    Text(
                        "게임 시작",
                        fontWeight = FontWeight.Bold,
                        fontSize = 16.sp
                    )
                }
            } else {
                Button(
                    onClick = onToggleReady,
                    colors = ButtonDefaults.buttonColors(
                        containerColor = if (isReady) GameColors.warning else GameColors.primary
                    ),
                    shape = RoundedCornerShape(16.dp),
                    modifier = Modifier.padding(horizontal = 32.dp)
                ) {
                    Text(
                        if (isReady) "준비 취소" else "준비",
                        fontWeight = FontWeight.Bold,
                        fontSize = 16.sp
                    )
                }
            }
        }

        // 파티 카드 위치 조정
        if (isPartyExpanded) {
            Box(
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .padding(bottom = 80.dp), // 버튼 위로 간격 확보
                contentAlignment = Alignment.Center
            ) {
                PartyDetailCard(
                    partyMembers = partyMembers,
                    onInviteFriendsClick = onInviteFriendsClick,
                    onCollapse = { isPartyExpanded = false }
                )
            }
        } else {
            Box(
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(end = 16.dp, bottom = 72.dp), // 게임 시작 버튼 오른쪽 위 (8dp 간격)
                contentAlignment = Alignment.Center
            ) {
                PartyCompactCard(
                    partyMembers = partyMembers,
                    onClick = { isPartyExpanded = true }
                )
            }
        }

    }
}

@Composable
private fun PartyCompactCard(
    partyMembers: List<PartyMember>,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .clickable { onClick() },
        colors = CardDefaults.cardColors(
            containerColor = Color(0xFF1A1D2E).copy(alpha = 0.9f)
        ),
        shape = RoundedCornerShape(16.dp),
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // 파티 아이콘
            Icon(
                Icons.Default.Group,
                contentDescription = "파티",
                tint = GameColors.primary,
                modifier = Modifier.size(20.dp)
            )

            Spacer(modifier = Modifier.height(4.dp))

            // 파티 인원수
            Text(
                text = "${partyMembers.size}/5",
                color = Color.White,
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold
            )

            Spacer(modifier = Modifier.height(8.dp))

            Row(
                horizontalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                repeat(5) { index ->
                    Box(
                        modifier = Modifier
                            .size(8.dp)
                            .clip(CircleShape)
                            .background(
                                if (index < partyMembers.size) {
                                    val member = partyMembers[index]
                                    when {
                                        member.isLeader -> Color(0xFFFFD700) // 방장은 금색
                                        member.isReady || member.isLeader -> GameColors.success // 레디 또는 방장
                                        else -> GameColors.warning // 레디 안됨
                                    }
                                } else {
                                    GameColors.surface // 빈 슬롯
                                }
                            )
                    )
                }
            }

        }
    }
}

@Composable
private fun PartyDetailCard(
    partyMembers: List<PartyMember>,
    onInviteFriendsClick: () -> Unit,
    onCollapse: () -> Unit
) {
    Card(
        colors = CardDefaults.cardColors(
            containerColor = Color(0xFF1A1D2E).copy(alpha = 0.95f)
        ),
        shape = RoundedCornerShape(20.dp)
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // 헤더
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = "파티 (${partyMembers.size}/5)",
                    color = Color.White,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )

                IconButton(
                    onClick = onCollapse,
                    modifier = Modifier.size(24.dp)
                ) {
                    Icon(
                        Icons.Default.Close,
                        contentDescription = "접기",
                        tint = GameColors.textSecondary,
                        modifier = Modifier.size(16.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            // 파티 멤버 목록
            LazyRow(
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                contentPadding = PaddingValues(horizontal = 8.dp)
            ) {
                items(partyMembers) { member ->
                    PartyMemberItem(
                        member = member,
                        size = 48.dp
                    )
                }

                // 초대 슬롯
                if (partyMembers.size < 5) {
                    item {
                        InviteSlot(
                            onClick = onInviteFriendsClick,
                            size = 48.dp
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun PartyMemberItem(
    member: PartyMember,
    size: Dp = 60.dp,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Box {
            // 프로필 아이콘
            Box(
                modifier = Modifier
                    .size(size)
                    .clip(CircleShape)
                    .background(
                        brush = Brush.radialGradient(
                            colors = listOf(
                                GameColors.primary,
                                GameColors.primaryLight
                            )
                        )
                    ),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = member.nickname.first().toString(),
                    color = Color.White,
                    fontSize = (size.value * 0.3f).sp,
                    fontWeight = FontWeight.Bold
                )
            }

            // 방장 표시
            if (member.isLeader) {
                Box(
                    modifier = Modifier
                        .size((size.value * 0.3f).dp)
                        .clip(CircleShape)
                        .background(Color(0xFFFFD700))
                        .align(Alignment.TopEnd),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "방",
                        color = Color.Black,
                        fontSize = (size.value * 0.15f).sp,
                        fontWeight = FontWeight.Bold
                    )
                }
            }

            // 레디 상태 표시
            if (!member.isLeader) {
                Box(
                    modifier = Modifier
                        .size((size.value * 0.25f).dp)
                        .clip(CircleShape)
                        .background(
                            if (member.isReady) GameColors.success else GameColors.warning
                        )
                        .align(Alignment.BottomEnd)
                )
            }
        }

        Spacer(modifier = Modifier.height(4.dp))

        Text(
            text = member.nickname,
            color = Color(0xFF85C1E9),
            fontSize = (size.value * 0.18f).sp,
            fontWeight = FontWeight.Medium
        )
    }
}

@Composable
private fun InviteSlot(
    onClick: () -> Unit,
    size: Dp = 60.dp,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        GameIconButton(
            onClick = onClick,
            icon = Icons.Default.PersonAdd,
            contentDescription = "초대",
            backgroundColor = GameColors.surface,
            iconColor = GameColors.textSecondary,
            size = size.value.toInt()
        )

        Spacer(modifier = Modifier.height(4.dp))

        Text(
            text = "초대",
            color = GameColors.textSecondary,
            fontSize = (size.value * 0.18f).sp,
            fontWeight = FontWeight.Medium
        )
    }
}

// 시간을 MM:SS 형식으로 포맷팅
private fun formatTime(seconds: Int): String {
    val minutes = seconds / 60
    val remainingSeconds = seconds % 60
    return String.format("%02d:%02d", minutes, remainingSeconds)
}