package com.ggumtle.auth

import androidx.compose.foundation.Image
import androidx.compose.foundation.gestures.detectTapGestures
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ColorFilter
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import com.ggumtle.auth.component.button.GoogleLoginButton
import com.ggumtle.auth.component.text.BlinkingText
import com.ggumtle.core.designsystem.R

@Composable
fun LoginContent(
    onGoogleLoginClick: () -> Unit,
    onNavigateToMain: () -> Unit,
    isLoading: Boolean = false,
    isLoginSuccess: Boolean = false,
    isNavigating: Boolean = false
) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .let { modifier ->
                if (isLoginSuccess) {
                    modifier.pointerInput(Unit) {
                        detectTapGestures { onNavigateToMain() }
                    }
                } else {
                    modifier
                }
            }
            .padding(16.dp),
        contentAlignment = Alignment.Center
    ) {
        if(!isNavigating){
            Image(
                painter = painterResource(id = R.drawable.ic_login_header),
                contentDescription = "로그인 헤더",
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .padding(top = 60.dp)
                    .size(200.dp)
            )

            if (isLoginSuccess) {
                BlinkingText(
                    text = "터치해서 시작",
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(horizontal = 16.dp)
                        .padding(bottom = 100.dp)            )
            } else {
                GoogleLoginButton(
                    onClick = onGoogleLoginClick,
                    isLoading = isLoading,
                    modifier = Modifier
                        .align(Alignment.BottomCenter)
                        .padding(horizontal = 16.dp)
                        .padding(bottom = 100.dp)
                )
            }
        }
    }
}