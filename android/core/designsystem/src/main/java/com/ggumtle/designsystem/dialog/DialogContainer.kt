package com.ggumtle.designsystem.dialog

import androidx.compose.runtime.Composable

@Composable
fun DialogContainer(
    dialogState: DialogState,
    onDismiss: () -> Unit = {}
) {
    when (dialogState) {
        is DialogState.Hidden -> {
            // 다이얼로그 숨김
        }

        is DialogState.SingleButtonDialog -> {
            SingleButtonDialog(
                dialogState = dialogState,
                onDismiss = onDismiss
            )
        }

        is DialogState.TwoButtonDialog -> {
            TwoButtonDialog(
                dialogState = dialogState,
                onDismiss = onDismiss
            )
        }
    }
}