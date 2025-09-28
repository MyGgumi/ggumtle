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
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.res.vectorResource
import androidx.compose.ui.unit.dp
import androidx.compose.ui.tooling.preview.Preview
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
    Box(
        modifier = modifier.padding(8.dp).fillMaxWidth()
    ) {
        // 파티 나가기 버튼 (오프셋으로 고정 위치)
        AnimatedVisibility(
            visible = isInParty,
            enter = fadeIn() + slideInVertically(),
            exit = fadeOut() + slideOutVertically(),
            modifier = Modifier
                .align(Alignment.TopEnd)
                .offset(x = (-56).dp) // 메뉴 버튼 크기만큼 왼쪽으로 오프셋
        ) {
            MenuButton(
                icon = ImageVector.vectorResource(id = R.drawable.btn_exit_party),
                contentDescription = "파티 나가기",
                onClick = onLeavePartyClick,
                backgroundColor = Color.Transparent,
                iconTint = Color.White
            )
        }

        // 메뉴 또는 드롭다운 (우측 끝에 고정)
        Box(
            modifier = Modifier.align(Alignment.TopEnd)
        ) {
            if (!isExpanded) {
                MenuButton(
                    icon = Icons.Default.Menu,
                    contentDescription = "메뉴 열기",
                    onClick = onTabClick,
                    backgroundColor = Color.Transparent,
                )
            } else {
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
                                    onClick = onSettingsClick,
                                    iconTint = Color.White
                                )

                                MenuButton(
                                    painter = painterResource(id = R.drawable.btn_game_invite),
                                    contentDescription = "초대 목록 확인",
                                    onClick = onInviteListClick,
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}

// MenuButton을 Painter도 받을 수 있도록 오버로드 추가
@Composable
private fun MenuButton(
    painter: androidx.compose.ui.graphics.painter.Painter,
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
            painter = painter,
            contentDescription = contentDescription,
            tint = Color.White,
            modifier = Modifier.size(28.dp)
        )
    }
}

@Composable
private fun MenuButton(
    icon: ImageVector,
    contentDescription: String,
    onClick: () -> Unit,
    backgroundColor: Color = Color.Transparent,
    iconTint: Color = Color.White,
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
            tint = iconTint,
            modifier = Modifier.size(28.dp)
        )
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun MenuTabCollapsedPreview() {
    MenuTab(
        isExpanded = false,
        isInParty = true,
        onTabClick = {},
        onSettingsClick = {},
        onInviteListClick = {},
        onLeavePartyClick = {},
        onSocialClick = {}
    )
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun MenuTabExpandedPreview() {
    MenuTab(
        isExpanded = true,
        isInParty = true,
        onTabClick = {},
        onSettingsClick = {},
        onInviteListClick = {},
        onLeavePartyClick = {},
        onSocialClick = {}
    )
}