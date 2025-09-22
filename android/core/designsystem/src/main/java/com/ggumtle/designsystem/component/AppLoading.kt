package com.ggumtle.designsystem.component

import androidx.compose.foundation.layout.size
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.ggumtle.designsystem.theme.DarkGray

@Composable
fun AppLoading(
    modifier: Modifier = Modifier,
    color: Color = DarkGray
) {
    CircularProgressIndicator(
        modifier = modifier.size(18.dp),
        strokeWidth = 2.dp,
        color = color
    )
}