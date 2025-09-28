package com.ggumtle.social.component

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.component.GameCard
import com.ggumtle.designsystem.component.button.GameIconButton
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.domain.websocket.model.FriendRequest
import com.ggumtle.core.designsystem.R

@Composable
fun GameFriendRequestItem(
    request: FriendRequest,
    onAccept: () -> Unit,
    onReject: () -> Unit
) {
    GameCard {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                modifier = Modifier
                    .size(56.dp)
                    .clip(CircleShape)
                    .background(
                        brush = Brush.radialGradient(
                            colors = listOf(
                                GameColors.primary,
                                GameColors.primaryLight.copy(alpha = 0.7f)
                            )
                        )
                    ),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = request.nickname.first().toString(),
                    color = Color.White,
                    fontSize = 20.sp,
                    fontWeight = FontWeight.Bold
                )
            }

            Spacer(modifier = Modifier.width(16.dp))

            Column(
                modifier = Modifier.weight(1f)
            ) {
                Text(
                    text = request.nickname,
                    color = GameColors.textPrimary,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )

                Text(
                    text = "친구 요청을 보냈습니다",
                    color = GameColors.textSecondary,
                    fontSize = 14.sp
                )
            }

            Row(
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Image(
                    painter = painterResource(id = R.drawable.btn_check),
                    contentDescription = "수락",
                    modifier = Modifier
                        .size(28.dp)
                        .clickable { onAccept() }
                )

                Image(
                    painter = painterResource(id = R.drawable.btn_cancel),
                    contentDescription = "거절",
                    modifier = Modifier
                        .size(28.dp)
                        .clickable { onReject() }
                )
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun GameFriendRequestItemPreview() {
    MaterialTheme {
        GameFriendRequestItem(
            request = FriendRequest(
                friendRequestId = 102L,
                memberId = 22L,
                nickname = "미라클킹"
            ),
            onAccept = { },
            onReject = { }
        )
    }
}