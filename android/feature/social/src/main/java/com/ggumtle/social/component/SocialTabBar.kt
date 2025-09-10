package com.ggumtle.social.component

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.designsystem.theme.GameColors
import com.ggumtle.social.SocialContract

@Composable
fun SocialTabBar(
    currentTab: SocialContract.SocialTab,
    onTabSelected: (SocialContract.SocialTab) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(
                brush = Brush.verticalGradient(
                    colors = listOf(
                        GameColors.background,
                        GameColors.surface.copy(alpha = 0.3f)
                    )
                )
            )
            .padding(horizontal = 20.dp, vertical = 20.dp),
        horizontalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        SocialTabItem(
            text = "친구 목록",
            icon = Icons.Default.People,
            isSelected = currentTab == SocialContract.SocialTab.FRIENDS,
            onSelected = { onTabSelected(SocialContract.SocialTab.FRIENDS) },
            modifier = Modifier.weight(1f)
        )
        SocialTabItem(
            text = "친구 추가",
            icon = Icons.Default.PersonAdd,
            isSelected = currentTab == SocialContract.SocialTab.ADD_FRIENDS,
            onSelected = { onTabSelected(SocialContract.SocialTab.ADD_FRIENDS) },
            modifier = Modifier.weight(1f)
        )
    }
}

@Composable
private fun SocialTabItem(
    text: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    isSelected: Boolean,
    onSelected: () -> Unit,
    modifier: Modifier = Modifier
) {
    val backgroundColor = if (isSelected) {
        Brush.horizontalGradient(
            colors = listOf(GameColors.primary, GameColors.primaryLight)
        )
    } else {
        Brush.horizontalGradient(
            colors = listOf(Color.Transparent, Color.Transparent)
        )
    }
    val textColor = if (isSelected) Color.White else GameColors.textSecondary

    Card(
        modifier = modifier
            .height(48.dp)
            .clickable { onSelected() },
        colors = CardDefaults.cardColors(
            containerColor = Color.Transparent
        ),
        shape = RoundedCornerShape(24.dp)
    ) {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(backgroundColor, RoundedCornerShape(24.dp))
                .padding(horizontal = 16.dp),
            contentAlignment = Alignment.Center
        ) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = text,
                    tint = textColor,
                    modifier = Modifier.size(18.dp)
                )
                Spacer(modifier = Modifier.width(8.dp))
                Text(
                    text = text,
                    color = textColor,
                    fontSize = 14.sp,
                    fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium
                )
            }
        }
    }
}