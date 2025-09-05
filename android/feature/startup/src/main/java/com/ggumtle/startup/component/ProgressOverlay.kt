package com.ggumtle.startup.component

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun ProgressOverlay(
    progress: Int,
    loadingMessage: String,
    isError: Boolean,
    errorMessage: String,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        if (isError) {
            ErrorContent(
                errorMessage = errorMessage
            )
        } else {
            LoadingContent(
                progress = progress,
                loadingMessage = loadingMessage
            )
        }
    }
}
@Composable
private fun LoadingContent(
    progress: Int,
    loadingMessage: String
) {
    Text(
        text = loadingMessage,
        fontSize = 16.sp,
        fontWeight = FontWeight.Medium,
        color = Color.White,
        textAlign = TextAlign.Center
    )

    Spacer(modifier = Modifier.height(16.dp))

    LinearProgressIndicator(
        progress = { progress / 100f },
        modifier = Modifier
            .fillMaxWidth()
            .height(4.dp),
        color = Color.White,
    )

    Spacer(modifier = Modifier.height(8.dp))

    Text(
        text = "$progress%",
        fontSize = 14.sp,
        color = Color.White.copy(alpha = 0.8f),
        textAlign = TextAlign.Center
    )
}

@Composable
private fun ErrorContent(
    errorMessage: String,
) {
    Text(
        text = "오류 발생",
        fontSize = 18.sp,
        fontWeight = FontWeight.Bold,
        color = Color.Red,
        textAlign = TextAlign.Center
    )

    Spacer(modifier = Modifier.height(8.dp))

    Text(
        text = errorMessage,
        fontSize = 14.sp,
        color = Color.White.copy(alpha = 0.8f),
        textAlign = TextAlign.Center
    )
}