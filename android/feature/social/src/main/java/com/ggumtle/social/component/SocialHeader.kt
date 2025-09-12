package com.ggumtle.social.component

import androidx.compose.foundation.background
import androidx.compose.ui.unit.*
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.*
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.*
import androidx.compose.ui.Modifier
import com.ggumtle.designsystem.theme.GameColors
import androidx.compose.ui.graphics.Color

@Composable
fun SocialHeader(
    title: String,
    onBackClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 12.dp)
            .background(GameColors.background)
    ) {
        IconButton(
            onClick = onBackClick,
            modifier = Modifier
                .align(Alignment.CenterStart)
                .size(32.dp)
        ) {
            Icon(
                imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                contentDescription = "뒤로가기",
                tint = GameColors.textPrimary
            )
        }

        Text(
            text = title,
            style = MaterialTheme.typography.titleLarge,
            color = GameColors.textPrimary,
            modifier = Modifier.align(Alignment.Center)
        )
    }
}