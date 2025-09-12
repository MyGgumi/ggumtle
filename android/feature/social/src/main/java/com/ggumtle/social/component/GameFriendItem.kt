package com.ggumtle.social.component

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.MoreVert
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
import com.ggumtle.designsystem.component.GameCard
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.domain.model.MemberConnectionState
import com.ggumtle.domain.websocket.model.Friend

@Composable
fun GameFriendItem(
    friend: Friend,
    onOpenProfile: () -> Unit,
    onRemoveFriend: () -> Unit
) {
    GameCard(
        onClick = onOpenProfile
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box {
                Box(
                    modifier = Modifier
                        .size(56.dp)
                        .clip(CircleShape)
                        .background(
                            brush = Brush.radialGradient(
                                colors = listOf(GameColors.primary, GameColors.primaryLight.copy(alpha = 0.7f))
                            )
                        ),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = friend.nickname.first().toString(),
                        color = Color.White,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold
                    )
                }

                if (friend.connectionState!= MemberConnectionState.OFFLINE) {
                    Box(
                        modifier = Modifier
                            .size(16.dp)
                            .clip(CircleShape)
                            .background(GameColors.online)
                            .align(Alignment.BottomEnd)
                    )
                }
            }

            Spacer(modifier = Modifier.width(16.dp))

            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = friend.nickname,
                    color = GameColors.textPrimary,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )

                if (friend.status != null) {
                    Text(
                        text = friend.status.toString(),
                        color = GameColors.textSecondary,
                        fontSize = 14.sp
                    )
                }
            }

            IconButton(
                onClick = onRemoveFriend
            ) {
                Icon(
                    Icons.Default.MoreVert,
                    contentDescription = "더보기",
                    tint = GameColors.textSecondary
                )
            }
        }
    }
}