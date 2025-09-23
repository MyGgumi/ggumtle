package com.ggumtle.ggumtle.navigation

import android.Manifest
import android.widget.Toast
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.runtime.*
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.ggumtle.auth.LoginRoute
import com.ggumtle.datastore.AuthManager
import com.ggumtle.datastore.LogoutReason
import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.startup.StartUpRoute
import com.ggumtle.home.HomeRoute
import com.ggumtle.social.SocialRoute
import com.ggumtle.growth.GrowthRoute
import com.ggumtle.mission.MissionRoute
import kotlinx.coroutines.delay

@Composable
fun AppNavigation(
    isLoggedIn: Boolean = false,
    authManager: AuthManager,
    unitySendManager: UnitySendManager,
    showUnity: (onComplete: () -> Unit) -> Unit,
    hideUnity: (onComplete: () -> Unit) -> Unit,
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
                LogoutReason.AccountDeleted -> "회원탈퇴가 완료되었습니다"
            }
            message?.let { Toast.makeText(context, it, Toast.LENGTH_SHORT).show() }
            unitySendManager.goToLoginFromHome()
            delay(2000)
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
                    navController.navigate(LoginRoute) {
                        popUpTo(StartUpRoute) { inclusive = true }
                    }
                }
            )
        }

        composable<LoginRoute> {
            LoginRoute(
                onNavigateToMain = {
                    navController.navigate(HomeRoute) {
                        popUpTo(LoginRoute) { inclusive = true }
                    }
                },
                isLoggedIn = isLoggedIn
            )
        }

        composable<HomeRoute> {
            HomeRoute(
                onNavigateToSocial = {
                    navController.navigate(SocialRoute){}
                },
                onNavigateToGrowth = {
                    navController.navigate(GrowthRoute){}
                }
            )
        }

        composable<SocialRoute> {
            SocialRoute(
                onNavigateToHome = {
                    navController.navigate(HomeRoute) {
                        popUpTo(SocialRoute) { inclusive = true }
                    }
                }
            )
        }

        composable<GrowthRoute>{
            val cameraPermissionLauncher = rememberLauncherForActivityResult(
                contract = ActivityResultContracts.RequestPermission()
            ) { isGranted ->
                if (isGranted) {
                    hideUnity {
                        navController.navigate(MissionRoute) {
                            popUpTo(GrowthRoute) { inclusive = true }
                        }
                    }
                } else {
                    Toast.makeText(context, "카메라 권한이 필요합니다", Toast.LENGTH_SHORT).show()
                }
            }

            GrowthRoute(
                onNavigateToHome = {
                    navController.navigate(HomeRoute) {
                        popUpTo(GrowthRoute) { inclusive = true }
                    }
                },
                onNavigateToMission = {
                    cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
                }
            )
        }

        composable<MissionRoute>{
            MissionRoute(
                onNavigateBack = {
                    showUnity{
                        navController.navigate(GrowthRoute){
                            popUpTo(MissionRoute){inclusive = true}
                        }
                    }
                }
            )
        }
    }
}