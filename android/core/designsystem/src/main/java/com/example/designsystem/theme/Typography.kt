package com.ggumtle.designsystem.theme

import androidx.compose.material3.Typography
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.Font
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp
import com.ggumtle.core.designsystem.R

/**
 * 앱 전용 폰트 패밀리
 */
val SuseonghyejeongFontFamily = FontFamily(
    Font(R.font.suseonghyejeong, FontWeight.Normal)
)

/**
 * 앱 전반에 사용할 텍스트 스타일들을 정의한 객체입니다.
 */
val Typography = Typography(
    displayLarge = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 36.sp
    ),
    headlineLarge = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 32.sp
    ),
    headlineMedium = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 28.sp
    ),
    titleLarge = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 22.sp
    ),
    bodyLarge = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 16.sp
    ),
    bodyMedium = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 14.sp
    ),
    bodySmall = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 12.sp
    ),
    labelLarge = TextStyle(
        fontFamily = SuseonghyejeongFontFamily,
        fontWeight = FontWeight.Normal,
        fontSize = 14.sp
    )
)