package com.ggumtle.ggumtle.navigation

import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.auth.LoginRoute
import com.ggumtle.startup.StartUpRoute
import com.ggumtle.ggumtle.navigation.bottomnav.MainBottomNavigationContainer

@Composable
fun AppNavigation(
    isLoggedIn: Boolean = false,
) {
    val navController = rememberNavController()
    val context = LocalContext.current

    NavHost(
        navController = navController,
        startDestination = StartUpRoute
    ) {
        composable<StartUpRoute> {
            StartUpRoute(
                onNavigateToLogin = {
                    navController.navigate(LoginRoute){
                        popUpTo(StartUpRoute) { inclusive = true }
                    }
                }
            )
        }

        composable<LoginRoute> {
            LoginRoute(
                onNavigateToMain = {
                    navController.navigate(MainRoute) {
                        popUpTo(LoginRoute) { inclusive = true }
                    }
                },
                isLoggedIn = isLoggedIn
            )
        }

        composable<MainRoute> {
            MainBottomNavigationContainer(
                onNavigateToAuth = {
                    navController.navigate(LoginRoute) {
                        popUpTo(MainRoute) { inclusive = true }
                    }
                },
            )
        }
    }
}