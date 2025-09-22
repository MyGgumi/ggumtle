package com.ggumtle.designsystem.theme

import androidx.compose.ui.graphics.Brush

/**
 * 재사용 가능한 그라디언트 정의
 */
object AppGradients {

    /**
     * 메인 브랜드 그라디언트 (가로)
     */
    val Primary = Brush.horizontalGradient(
        colors = listOf(
            BrandColors.GradientStart,
            BrandColors.GradientEnd
        )
    )

    /**
     * 반대 방향 그라디언트 (핑크 -> 보라)
     */
    val PrimaryReverse = Brush.horizontalGradient(
        colors = listOf(
            BrandColors.GradientEnd,
            BrandColors.GradientStart
        )
    )

    /**
     * 버튼용 그라디언트 (약간 투명도 적용)
     */
    val ButtonPrimary = Brush.horizontalGradient(
        colors = listOf(
            BrandColors.GradientStart.copy(alpha = 0.9f),
            BrandColors.GradientEnd.copy(alpha = 0.9f)
        )
    )

    /**
     * 배경용 그라디언트
     */
    val Background = Brush.verticalGradient(
        colors = listOf(
            BrandColors.PurpleDark,
            BrandColors.PurpleLight
        )
    )
}