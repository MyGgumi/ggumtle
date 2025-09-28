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
import com.ggumtle.ingame.InGameRoute
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
                is LogoutReason.UserLogout -> "로그아웃되었습니다"
                is LogoutReason.TokenExpired -> "세션이 만료되어 다시 로그인해 주세요"
                is LogoutReason.NetworkError -> "네트워크 오류로 인해 로그아웃되었습니다"
                is LogoutReason.SessionExpired -> "세션이 만료되었습니다. 다시 로그인해 주세요"
                is LogoutReason.AccountDeleted -> "회원탈퇴가 완료되었습니다"
            }
            message.let { Toast.makeText(context, it, Toast.LENGTH_SHORT).show() }
            unitySendManager.goToLoginFromHome()
            delay(2000)
            navController.navigate(LoginDestination) {
                popUpTo(0) { inclusive = true }
            }
        }
    }

    NavHost(
        navController = navController,
        startDestination = StartUpDestination
    ) {
        composable<StartUpDestination> {
            StartUpRoute(
                onNavigateToLogin = {
                    navController.navigate(LoginDestination) {
                        popUpTo(StartUpDestination) { inclusive = true }
                    }
                }
            )
        }

        composable<LoginDestination> {
            LoginRoute(
                onNavigateToMain = {
                    navController.navigate(HomeDestination(fromLogin = true)) {
                        popUpTo(LoginDestination) { inclusive = true }
                    }
                },
                isLoggedIn = isLoggedIn
            )
        }

        composable<HomeDestination> {
            HomeRoute(
                onNavigateToSocial = {
                    navController.navigate(SocialDestination)
                },
                onNavigateToGrowth = {
                    navController.navigate(GrowthDestination)
                },
                onNavigateToInGame = {
                    navController.navigate(InGameDestination)
                }
            )
        }

        composable<SocialDestination> {
            SocialRoute(
                onNavigateToHome = {
                    navController.navigate(HomeDestination(fromLogin = false)) {
                        popUpTo(HomeDestination::class) { inclusive = true }
                    }
                }
            )
        }

        composable<GrowthDestination>{
            val cameraPermissionLauncher = rememberLauncherForActivityResult(
                contract = ActivityResultContracts.RequestPermission()
            ) { isGranted ->
                if (isGranted) {
                    hideUnity {
                        navController.navigate(MissionDestination)
                    }
                } else {
                    Toast.makeText(context, "카메라 권한이 필요합니다", Toast.LENGTH_SHORT).show()
                }
            }

            GrowthRoute(
                onNavigateToHome = {
                    navController.navigate(HomeDestination(fromLogin = false)) {
                        popUpTo(HomeDestination::class) { inclusive = true }
                    }
                },
                onNavigateToMission = {
                    cameraPermissionLauncher.launch(Manifest.permission.CAMERA)
                }
            )
        }

        composable<MissionDestination>{
            MissionRoute(
                onNavigateBack = {
                    showUnity{
                        navController.navigate(GrowthDestination) {
                            popUpTo(GrowthDestination) { inclusive = false }
                        }
                    }
                }
            )
        }

        composable<InGameDestination>{
            InGameRoute(

            )
        }
    }
}