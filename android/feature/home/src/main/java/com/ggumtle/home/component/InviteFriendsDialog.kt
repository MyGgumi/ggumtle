package com.ggumtle.home.component

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.designsystem.component.button.GameIconButton
import com.example.designsystem.theme.GameColors
import com.ggumtle.home.model.Friend

@Composable
fun InviteFriendsDialog(
    friends: List<Friend>,
    partyMemberIds: List<String>,
    isLoading: Boolean,
    onDismiss: () -> Unit,
    onInviteFriend: (String) -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .heightIn(max = 600.dp)
                .padding(16.dp),
            colors = CardDefaults.cardColors(
                containerColor = Color(0xFF1A1D2E)
            ),
            shape = RoundedCornerShape(20.dp)
        ) {
            Column(
                modifier = Modifier.padding(20.dp)
            ) {
                // 헤더
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "친구 초대",
                        color = Color(0xFF85C1E9),
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold
                    )

                    IconButton(onClick = onDismiss) {
                        Icon(
                            Icons.Default.Close,
                            contentDescription = "닫기",
                            tint = GameColors.textSecondary
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // 친구 목록
                val availableFriends = friends.filter { friend ->
                    friend.id !in partyMemberIds
                }

                if (availableFriends.isEmpty()) {
                    // 초대할 친구가 없는 경우
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(200.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                Icons.Default.PersonAdd,
                                contentDescription = null,
                                tint = GameColors.textSecondary,
                                modifier = Modifier.size(48.dp)
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Text(
                                text = "초대할 수 있는 친구가 없습니다",
                                color = GameColors.textSecondary,
                                fontSize = 16.sp
                            )
                        }
                    }
                } else {
                    LazyColumn(
                        modifier = Modifier.heightIn(max = 400.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(availableFriends) { friend ->
                            FriendInviteItem(
                                friend = friend,
                                isLoading = isLoading,
                                onInvite = { onInviteFriend(friend.id) }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun FriendInviteItem(
    friend: Friend,
    isLoading: Boolean,
    onInvite: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = GameColors.surface
        ),
        shape = RoundedCornerShape(12.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // 프로필 아이콘
            Box {
                Box(
                    modifier = Modifier
                        .size(48.dp)
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
                        text = friend.nickname.first().toString(),
                        color = Color.White,
                        fontSize = 18.sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                // 온라인 상태 표시
                if (friend.isOnline) {
                    Box(
                        modifier = Modifier
                            .size(14.dp)
                            .clip(CircleShape)
                            .background(GameColors.online)
                            .align(Alignment.BottomEnd)
                    )
                }
            }

            Spacer(modifier = Modifier.width(12.dp))

            // 친구 정보
            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = friend.nickname,
                    color = Color.White,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Medium
                )
                Text(
                    text = if (friend.isOnline) "온라인" else "오프라인",
                    color = if (friend.isOnline) GameColors.online else GameColors.textSecondary,
                    fontSize = 12.sp
                )
            }

            // 초대 버튼
            if (friend.isOnline) {
                GameIconButton(
                    onClick = onInvite,
                    icon = Icons.Default.PersonAdd,
                    contentDescription = "초대",
                    backgroundColor = GameColors.primary,
                    size = 40,
                    modifier = Modifier.then(
                        if (isLoading) Modifier else Modifier
                    )
                )
            } else {
                GameIconButton(
                    onClick = { },
                    icon = Icons.Default.PersonAdd,
                    contentDescription = "초대 불가",
                    backgroundColor = GameColors.textSecondary,
                    iconColor = GameColors.surface,
                    size = 40
                )
            }
        }
    }
}