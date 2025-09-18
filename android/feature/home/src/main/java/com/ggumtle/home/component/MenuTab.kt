package com.ggumtle.home.component

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInVertically
import androidx.compose.animation.slideOutVertically
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.res.vectorResource
import androidx.compose.ui.unit.dp
import com.ggumtle.core.designsystem.R

@Composable
fun MenuTab(
    isExpanded: Boolean,
    isInParty: Boolean,
    onTabClick: () -> Unit,
    onSettingsClick: () -> Unit,
    onInviteListClick: () -> Unit,
    onLeavePartyClick: () -> Unit,
    onSocialClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier.padding(8.dp),
        verticalAlignment = Alignment.Top
    ) {
        // 파티 나가기 버튼 (파티에 소속된 경우만 표시) TODO: isInParty로 최종적으로 수정 필요
        AnimatedVisibility(
            visible = true,
            enter = fadeIn() + slideInVertically(),
            exit = fadeOut() + slideOutVertically()
        ) {
            MenuButton(
                icon = ImageVector.vectorResource(id = R.drawable.btn_exit_party),
                contentDescription = "파티 나가기",
                onClick = onLeavePartyClick,
                backgroundColor = Color.Transparent,
            )
        }

        if (!isExpanded) {
            MenuButton(
                icon = Icons.Default.Menu,
                contentDescription = "메뉴 열기",
                onClick = onTabClick,
                backgroundColor = Color.Transparent,
            )
        }else{
            Column(
                horizontalAlignment = Alignment.End
            ) {
                // 드롭다운 메뉴
                AnimatedVisibility(
                    visible = isExpanded,
                    enter = fadeIn() + slideInVertically(initialOffsetY = { -it }),
                    exit = fadeOut() + slideOutVertically(targetOffsetY = { -it })
                ) {
                    Card(
                        modifier = Modifier.padding(bottom = 8.dp),
                        colors = CardDefaults.cardColors(
                            containerColor = Color(0xFF1A1D2E).copy(alpha = 0.5f)
                        ),
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        Column(
                            modifier = Modifier.padding(8.dp),
                            verticalArrangement = Arrangement.spacedBy(4.dp)
                        ) {
                            // 닫기 버튼 (맨 위)
                            MenuButton(
                                icon = Icons.Default.Close,
                                contentDescription = "메뉴 닫기",
                                onClick = onTabClick
                            )

                            MenuButton(
                                icon = Icons.Default.Settings,
                                contentDescription = "설정",
                                onClick = onSettingsClick
                            )

                            MenuButton(
                                icon = Icons.Default.Mail,
                                contentDescription = "초대 목록 확인",
                                onClick = onInviteListClick
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun MenuButton(
    icon: ImageVector,
    contentDescription: String,
    onClick: () -> Unit,
    backgroundColor: Color = Color.Transparent,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .size(48.dp)
            .clip(CircleShape)
            .background(backgroundColor)
            .clickable { onClick() },
        contentAlignment = Alignment.Center
    ) {
        Icon(
            imageVector = icon,
            contentDescription = contentDescription,
            tint = Color.White,
            modifier = Modifier.size(24.dp)
        )
    }
}