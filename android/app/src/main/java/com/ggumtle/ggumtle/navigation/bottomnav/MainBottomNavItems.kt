package com.ggumtle.ggumtle.navigation.bottomnav


//noinspection SuspiciousImport
import android.R
import com.example.designsystem.component.BottomNavItem
import com.ggumtle.ggumtle.navigation.HomeTabRoute
import com.ggumtle.ggumtle.navigation.Tab1Route
import com.ggumtle.ggumtle.navigation.Tab2Route

fun getMainBottomItems() = listOf(
    BottomNavItem(
        label = "탭1",
        iconRes = R.drawable.ic_menu_add,
        iconSelectedRes = R.drawable.ic_menu_add,
        route = Tab1Route::class.qualifiedName
    ),
    BottomNavItem(
        label = "홈",
        iconRes = R.drawable.ic_menu_myplaces,
        iconSelectedRes = R.drawable.ic_menu_myplaces,
        route = HomeTabRoute::class.qualifiedName
    ),
    BottomNavItem(
        label = "탭2",
        iconRes = R.drawable.ic_menu_info_details,
        iconSelectedRes = R.drawable.ic_menu_info_details,
        route = Tab2Route::class.qualifiedName
    )
)