package com.ggumtle.designsystem.component

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.material3.Text

/**
 * 보라색 검은색 박스 컴포넌트
 * - 라운드: 16dp
 * - 배경: #000000 (투명도 0.5)
 * - 테두리: 1dp, #C9B6E9 (투명도 0.3)
 * - 기본 패딩: 16dp
 */
@Composable
fun PurpleBlackBox(
    modifier: Modifier = Modifier,
    contentPadding: PaddingValues = PaddingValues(20.dp),
    contentAlignment: Alignment = Alignment.TopStart,
    content: @Composable BoxScope.() -> Unit
) {
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(16.dp))
            .background(Color.Black.copy(alpha = 0.5f))
            .border(
                width = 1.dp,
                color = Color(0xFFC9B6E9).copy(alpha = 0.3f),
                shape = RoundedCornerShape(16.dp)
            )
            .padding(contentPadding),
        contentAlignment = contentAlignment
    ) {
        content()
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF2D1B69)
@Composable
fun PurpleBlackBoxPreview() {
    PurpleBlackBox(
        modifier = Modifier
            .fillMaxWidth()
            .height(100.dp)
    ) {
        Text(
            text = "Purple Black Box Content",
            color = Color.White
        )
    }
}