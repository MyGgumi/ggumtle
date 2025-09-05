package com.ggumtle.startup

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import com.ggumtle.core.designsystem.R
import com.ggumtle.startup.component.ProgressOverlay

@Composable
fun StartUpContent(
    progress: Int,
    loadingMessage: String,
    isError: Boolean,
    errorMessage: String,
    modifier: Modifier = Modifier
) {
    Box(modifier = modifier.fillMaxSize()) {
        Image(
            painter = painterResource(R.drawable.screen_startup),
            contentDescription = null,
            modifier = Modifier.fillMaxSize(),
            contentScale = ContentScale.Crop
        )

        ProgressOverlay(
            progress = progress,
            loadingMessage = loadingMessage,
            isError = isError,
            errorMessage = errorMessage,
            modifier = Modifier.align(Alignment.BottomCenter)
        )
    }
}