package com.ggumtle.ggumtle.navigation

import android.widget.Toast
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.example.auth.LoginRoute
import com.example.datastore.AuthManager
import com.example.datastore.LogoutReason
import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.model.UnityMethod
import com.example.domain.unity.model.UnityTarget
import com.ggumtle.startup.StartUpRoute
import com.ggumtle.ggumtle.navigation.bottomnav.MainBottomNavigationContainer

@Composable
fun AppNavigation(
    isLoggedIn: Boolean = false,
    authManager: AuthManager,
    unitySendManager: UnitySendManager
) {
    val navController = rememberNavController()
    val context = LocalContext.current

    LaunchedEffect(Unit) {
        authManager.logoutEvent.collect { reason ->
            val message = when (reason) {
                LogoutReason.UserLogout -> "로그아웃되었습니다"
                LogoutReason.TokenExpired -> "세션이 만료되어 다시 로그인해 주세요"
                LogoutReason.NetworkError -> "네트워크 오류로 인해 로그아웃되었습니다"
                is LogoutReason.SessionExpired -> "세션이 만료되었습니다. 다시 로그인해 주세요"
            }
            Toast.makeText(context, message, Toast.LENGTH_SHORT).show()
            unitySendManager.sendToUnity(
                UnityTarget.ANDROID_UNITY_CONTROLLER.value,
                UnityMethod.START_REVERSE.value
            )
            navController.navigate(LoginRoute) {
                popUpTo(0) { inclusive = true }
            }
        }
    }

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