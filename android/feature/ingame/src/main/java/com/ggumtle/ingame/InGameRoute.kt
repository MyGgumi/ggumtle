package com.ggumtle.ingame

import android.app.Activity
import android.content.pm.ActivityInfo
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.ui.platform.LocalContext

@Composable
fun InGameRoute() {
    val activity = LocalContext.current as Activity

    DisposableEffect(Unit) {
        val currentOrientation = activity.requestedOrientation

        if (currentOrientation != ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE) {
            activity.runOnUiThread {
                activity.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_LANDSCAPE
            }
        }

        onDispose {
            activity.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED
        }
    }
}