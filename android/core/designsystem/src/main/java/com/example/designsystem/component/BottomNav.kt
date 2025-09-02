package com.example.designsystem.component

import androidx.annotation.DrawableRes
import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.size
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
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
        containerColor = Color(0xFF2B2B2B),
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
                            .fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Image(
                            painter = painterResource(
                                id = if (selected) item.iconSelectedRes else item.iconRes
                            ),
                            contentDescription = item.label,
                            modifier = Modifier
                                .size(40.dp)
                        )
                    }

                },
                label = {
                    if (item.label.isNotEmpty()) {
                        Text(item.label)
                    }
                },
                alwaysShowLabel = item.label.isNotEmpty()
            )
        }
    }
}