package com.ggumtle.ggumtle.navigation.bottomnav

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.ggumtle.ggumtle.navigation.HomeTabRoute
import com.ggumtle.ggumtle.navigation.Tab1Route
import com.ggumtle.ggumtle.navigation.Tab2Route
import com.unity3d.player.UnityPlayer

/**
 * 바텀 네비게이션을 포함한 메인 컨테이너
 */
@Composable
fun MainBottomNavigationContainer(
    onNavigateToAuth: () -> Unit = {},
) {
    val bottomNavController = rememberNavController()
    Scaffold(
        containerColor = Color.Transparent,
        bottomBar = {
            MainBottomNavigationBar(navController = bottomNavController)
        }
    ) { innerPadding ->
        NavHost(
            navController = bottomNavController,
            startDestination = HomeTabRoute,
            modifier = Modifier.padding(innerPadding)
        ) {
            composable<Tab1Route> {
                Tab1Screen()
            }
            composable<Tab2Route> {
                Tab2Screen()
            }
            composable<HomeTabRoute> {
                HomeScreen()
            }
        }
    }
}

@Composable
fun HomeScreen() {
    var currentPlayer by remember { mutableStateOf(1) }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Transparent),
        contentAlignment = Alignment.Center
    ) {

        Row(
            modifier = Modifier
                .align(Alignment.BottomEnd)
                .padding(16.dp)
        ) {
            Button(onClick = {
                if (currentPlayer <= 5) {
                    UnityPlayer.UnitySendMessage("AndroidUnityController", "AddCharacterByNickname", "player$currentPlayer")
                    currentPlayer++
                }
            }) {
                Text("추가")
            }
            Button(onClick = {
                if (currentPlayer > 1) {
                    currentPlayer--
                    UnityPlayer.UnitySendMessage("AndroidUnityController", "RemoveCharacterByNickname", "player$currentPlayer")
                }
            }) {
                Text("삭제")
            }
        }
    }
}


@Composable
fun Tab1Screen() {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E)),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "탭1",
            color = Color.White,
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center
        )
    }
}

@Composable
fun Tab2Screen() {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E1E1E)),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = "탭2",
            color = Color.White,
            fontSize = 24.sp,
            fontWeight = FontWeight.Bold,
            textAlign = TextAlign.Center
        )
    }
}
