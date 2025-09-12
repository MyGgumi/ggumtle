package com.ggumtle.growth

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.ggumtle.designsystem.theme.GameColors

@Composable
fun GrowthContent(
    state: GrowthContract.State,
    onBackClick: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        // 백 버튼
        IconButton(
            onClick = onBackClick,
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(top = 8.dp)
        ) {
            Icon(
                imageVector = Icons.AutoMirrored.Filled.ArrowBack,
                contentDescription = "홈으로 돌아가기",
                tint = GameColors.textPrimary
            )
        }
    }
}