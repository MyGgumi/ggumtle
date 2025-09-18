package com.ggumtle.home.component

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.core.designsystem.R
import com.ggumtle.home.model.UserProfile

@Composable
fun ProfileSection(
    userProfile: UserProfile,
    onClick: () -> Unit,
    coin: Int,
    modifier: Modifier = Modifier
) {
    Card(
        modifier = modifier
            .clickable { onClick() },
        colors = CardDefaults.cardColors(
            containerColor = Color.Transparent
        ),
        shape = RoundedCornerShape(20.dp)
    ) {
        Row(
            modifier = Modifier.padding(8.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(44.dp)
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
                // TODO: 프로필 이미지 로드
                Icon(
                    Icons.Default.Person,
                    contentDescription = "프로필",
                    tint = Color.White,
                    modifier = Modifier.size(24.dp)
                )
            }

            Column{
                Text(
                    text = userProfile.nickname,
                    color = Color.White,
                    fontSize = 14.sp,
                    fontWeight = FontWeight.Medium
                )

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Icon(
                        painter = painterResource(id = R.drawable.ic_coin),
                        contentDescription = "포인트",
                        tint = Color.Unspecified,
                        modifier = Modifier
                            .size(32.dp)
                            .offset(x = (-8).dp)
                    )

                    Text(
                        text = coin.toString(),
                        color = Color.White,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Normal,
                        modifier = Modifier.offset(x = (-8).dp)
                    )
                }
            }
        }
    }
}