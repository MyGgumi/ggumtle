package com.ggumtle.domain.unity

import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

interface UnityStartupManager {
    val progressFlow: StateFlow<Int>
    val messageFlow: StateFlow<String>

    val completionFlow: SharedFlow<Unit>
    val errorFlow: SharedFlow<String>

    fun updateProgress(progress: Int, message: String)
    fun completeLoading()
    fun reportError(errorMessage: String)
}