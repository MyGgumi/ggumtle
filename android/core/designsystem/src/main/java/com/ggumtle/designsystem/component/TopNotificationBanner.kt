package com.ggumtle.designsystem.component

import androidx.compose.animation.*
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.zIndex
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.domain.model.InviteNotification
import com.ggumtle.domain.model.NotificationType

@Composable
fun TopNotificationBanner(
    notification: InviteNotification,
    onDismiss: () -> Unit,
    onTap: (() -> Unit)? = null,
    modifier: Modifier = Modifier
) {
    AnimatedVisibility(
        visible = true,
        enter = slideInVertically(
            initialOffsetY = { -it },
            animationSpec = tween(300)
        ) + fadeIn(animationSpec = tween(300)),
        exit = slideOutVertically(
            targetOffsetY = { -it },
            animationSpec = tween(300)
        ) + fadeOut(animationSpec = tween(300)),
        modifier = modifier.zIndex(1000f)
    ) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 8.dp)
                .clickable { onTap?.invoke() },
            colors = CardDefaults.cardColors(
                containerColor = getNotificationColor(notification.type)
            ),
            shape = RoundedCornerShape(12.dp),
            elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(16.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // 아이콘
                Icon(
                    imageVector = getNotificationIcon(notification.type),
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(24.dp)
                )
                
                Spacer(modifier = Modifier.width(12.dp))
                
                // 메시지
                Text(
                    text = notification.message,
                    color = Color.White,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium,
                    modifier = Modifier.weight(1f)
                )
                
                Spacer(modifier = Modifier.width(8.dp))
                
                // 닫기 버튼
                IconButton(
                    onClick = onDismiss,
                    modifier = Modifier.size(32.dp)
                ) {
                    Icon(
                        Icons.Default.Close,
                        contentDescription = "알림 닫기",
                        tint = Color.White.copy(alpha = 0.8f),
                        modifier = Modifier.size(18.dp)
                    )
                }
            }
        }
    }
}

@Composable
private fun getNotificationColor(type: NotificationType): Color {
    return when (type) {
        NotificationType.PARTY_INVITE -> GameColors.primary
        NotificationType.FRIEND_REQUEST -> GameColors.success
        NotificationType.GAME_START -> GameColors.warning
        NotificationType.SYSTEM_MESSAGE -> Color(0xFF6B7280)
    }
}

private fun getNotificationIcon(type: NotificationType): ImageVector {
    return when (type) {
        NotificationType.PARTY_INVITE -> Icons.Default.Group
        NotificationType.FRIEND_REQUEST -> Icons.Default.PersonAdd
        NotificationType.GAME_START -> Icons.Default.PlayArrow
        NotificationType.SYSTEM_MESSAGE -> Icons.Default.Info
    }
}

@Composable
fun GlobalNotificationOverlay(
    notification: InviteNotification?,
    onDismiss: () -> Unit,
    onTap: (() -> Unit)? = null,
    modifier: Modifier = Modifier
) {
    if (notification != null) {
        Box(
            modifier = modifier.fillMaxSize(),
            contentAlignment = Alignment.TopCenter
        ) {
            TopNotificationBanner(
                notification = notification,
                onDismiss = onDismiss,
                onTap = onTap
            )
        }
    }
}