package com.example.designsystem.dialog

sealed class DialogState {
    data object Hidden : DialogState()

    data class SingleButtonDialog(
        val title: String? = null,
        val content: String,
        val buttonText: String = "확인",
        val onConfirm: () -> Unit = {}
    ) : DialogState()

    data class TwoButtonDialog(
        val title: String? = null,
        val content: String,
        val confirmText: String = "확인",
        val cancelText: String = "취소",
        val onConfirm: () -> Unit = {},
        val onCancel: () -> Unit = {}
    ) : DialogState()
}