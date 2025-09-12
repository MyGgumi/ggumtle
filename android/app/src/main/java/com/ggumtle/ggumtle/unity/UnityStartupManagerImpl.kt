package com.ggumtle.ggumtle.unity

import android.util.Log
import com.ggumtle.domain.unity.UnityStartupManager
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnityStartupManagerImpl @Inject constructor() : UnityStartupManager {

    private val _progressFlow = MutableStateFlow(0)
    override val progressFlow: StateFlow<Int> = _progressFlow.asStateFlow()

    private val _messageFlow = MutableStateFlow("Unity 초기화 중...")
    override val messageFlow: StateFlow<String> = _messageFlow.asStateFlow()

    private val _completionFlow = MutableSharedFlow<Unit>(replay = 1)
    override val completionFlow: SharedFlow<Unit> = _completionFlow.asSharedFlow()

    private val _errorFlow = MutableSharedFlow<String>(replay = 1)
    override val errorFlow: SharedFlow<String> = _errorFlow.asSharedFlow()

    override fun updateProgress(progress: Int, message: String) {
        Log.d("UnityStartupManager", "Progress 업데이트: $progress%, $message")
        _progressFlow.value = progress
        _messageFlow.value = message
    }

    override fun completeLoading() {
        Log.d("UnityStartupManager", "로딩 완료 처리")
        _completionFlow.tryEmit(Unit)
    }

    override fun reportError(errorMessage: String) {
        Log.e("UnityStartupManager", "에러 발생: $errorMessage")
        _errorFlow.tryEmit(errorMessage)
    }
}