package com.ggumtle.ggumtle.navigation.bottomnav


import androidx.compose.runtime.Composable
import androidx.navigation.NavController
import androidx.navigation.compose.currentBackStackEntryAsState
import com.example.designsystem.component.BottomNavBar

@Composable
fun MainBottomNavigationBar(
    navController: NavController,
) {
    val currentRoute = navController.currentBackStackEntryAsState().value?.destination?.route
    BottomNavBar(
        items = getMainBottomItems(),
        currentRoute = currentRoute,
        onNavigate = { route ->
            if (route != currentRoute) {
                navController.navigate(route) {
                    popUpTo(navController.graph.startDestinationId) { saveState = true }
                    launchSingleTop = true
                    restoreState = true
                }
            }
        }
    )
}

