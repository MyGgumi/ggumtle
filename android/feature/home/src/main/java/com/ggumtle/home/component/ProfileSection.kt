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
import androidx.compose.ui.tooling.preview.Preview
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.core.designsystem.R
import com.ggumtle.home.model.UserProfile
import java.text.DecimalFormat

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
                Text(
                    text = userProfile.nickname.first().toString(),
                    color = Color.White,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold
                )
            }

            Column{
                Text(
                    text = userProfile.nickname,
                    color = Color.White,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Medium
                )

                Spacer(modifier = Modifier.height(4.dp))

                Row(
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Icon(
                        painter = painterResource(id = R.drawable.ic_dream_coin),
                        contentDescription = "포인트",
                        tint = Color.Unspecified,
                        modifier = Modifier
                            .size(20.dp)
                    )

                    Text(
                        text = DecimalFormat("#,###").format(coin),
                        color = Color.White,
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Normal,
                        modifier = Modifier.offset(x = (6).dp)
                    )
                }
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun ProfileSectionPreview() {
    ProfileSection(
        userProfile = UserProfile(
            id = 1L,
            nickname = "꿈틀이",
            profileImageUrl = null
        ),
        coin = 15420,
        onClick = {}
    )
}