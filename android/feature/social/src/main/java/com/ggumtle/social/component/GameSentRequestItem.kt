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
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.component.GameCard
import com.ggumtle.designsystem.component.button.GameIconButton
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.domain.websocket.model.SentRequest

@Composable
fun GameSentRequestItem(
    request: SentRequest,
    onCancel: () -> Unit,
    onOpenProfile: () -> Unit
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
                    text = request.toUserName.first().toString(),
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
                    text = request.toUserName,
                    color = GameColors.textPrimary,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )

                Text(
                    text = "요청 대기중...",
                    color = GameColors.textSecondary,
                    fontSize = 14.sp
                )
            }

            Image(
                painter = painterResource(id = com.ggumtle.core.designsystem.R.drawable.btn_cancel),
                contentDescription = "취소",
                modifier = Modifier
                    .size(28.dp)
                    .clickable { onCancel() }
            )
        }
    }
}