// designsystem/component/GameBottomNavBar.kt
package com.example.designsystem.component

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.blur
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.designsystem.theme.GameColors
import kotlin.collections.forEach
import kotlin.text.isNotEmpty

data class BottomNavItem(
    val route: String?,
    @DrawableRes val iconRes: Int,
    @DrawableRes val iconSelectedRes: Int,
    val label: String = ""
)

@Composable
fun BottomNavBar(
    items: List<BottomNavItem>,
    currentRoute: String?,
    onNavigate: (String) -> Unit,
) {
    NavigationBar(
        containerColor = Color(0xFF1A1D2E).copy(alpha = 0.85f),
    ) {
        items.forEach { item ->
            val selected = currentRoute == item.route
            NavigationBarItem(
                selected = selected,
                onClick = { onNavigate(requireNotNull(item.route)) },
                icon = {
                    Box(
                        modifier = Modifier
                            .size(43.dp)
                            .fillMaxSize()
                            .then(
                                if (selected) {
                                    Modifier
                                        .clip(CircleShape)
                                        .background(
                                            brush = Brush.radialGradient(
                                                colors = listOf(
                                                    Color(0xFF4A90E2).copy(alpha = 0.3f), // 부드러운 블루 글로우
                                                    Color(0xFF2E86C1).copy(alpha = 0.1f),
                                                    Color.Transparent
                                                ),
                                                radius = 60f
                                            )
                                        )
                                } else {
                                    Modifier
                                }
                            ),
                        contentAlignment = Alignment.Center
                    ) {
                        Image(
                            painter = painterResource(
                                id = if (selected) item.iconSelectedRes else item.iconRes
                            ),
                            contentDescription = item.label,
                            modifier = Modifier.size(if (selected) 26.dp else 22.dp),
                            alpha = if (selected) 1f else 0.7f // 선택되지 않은 아이콘은 살짝 투명하게
                        )
                    }
                },
                label = {
                    if (item.label.isNotEmpty()) {
                        Text(
                            text = item.label,
                            color = if (selected) Color(0xFF85C1E9) else Color(0xFF8B9DC3), // 더 자연스러운 색상
                            fontSize = 10.sp,
                            fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal
                        )
                    }
                },
                alwaysShowLabel = item.label.isNotEmpty(),
                colors = NavigationBarItemDefaults.colors(
                    selectedIconColor = Color.White,
                    unselectedIconColor = Color(0xFF8B9DC3),
                    selectedTextColor = Color(0xFF85C1E9),
                    unselectedTextColor = Color(0xFF8B9DC3),
                    indicatorColor = Color.Transparent
                )
            )
        }
    }
}