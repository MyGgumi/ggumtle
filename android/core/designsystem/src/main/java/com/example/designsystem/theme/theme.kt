package com.example.designsystem.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

/**
 * 앱 전용 테마 컴포저블입니다.
 * 고급스럽고 긴장감 있는 다크 테마를 제공합니다.
 */
private val DarkColorScheme = darkColorScheme(
    onPrimary = White,
    onPrimaryContainer = White,

    onSecondary = Black,
    onSecondaryContainer = White,

    onTertiary = Black,

    onError = White,
    onErrorContainer = White,

)

@Composable
fun AppTheme(
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = DarkColorScheme,
        typography = Typography,
        content = content
    )
}