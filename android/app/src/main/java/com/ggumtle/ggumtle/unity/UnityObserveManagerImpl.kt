package com.ggumtle.ggumtle.unity

import android.util.Log
import com.ggumtle.domain.unity.UnityObserveManager
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnityObserveManagerImpl @Inject constructor() : UnityObserveManager {

    private val _progressFlow = MutableStateFlow(0)
    override val progressFlow: StateFlow<Int> = _progressFlow.asStateFlow()

    private val _messageFlow = MutableStateFlow("Unity 초기화 중...")
    override val messageFlow: StateFlow<String> = _messageFlow.asStateFlow()

    private val _completionFlow = MutableSharedFlow<Unit>(replay = 1)
    override val completionFlow: SharedFlow<Unit> = _completionFlow.asSharedFlow()

    private val _goToInGameFlow = MutableSharedFlow<Unit>(replay = 1)
    override val goToInGameFlow: SharedFlow<Unit> = _goToInGameFlow.asSharedFlow()

    private val _characterTypeChangeFlow = MutableSharedFlow<String>(replay = 1)
    override val characterTypeChangeFlow: SharedFlow<String> = _characterTypeChangeFlow.asSharedFlow()

    override fun updateProgress(progress: Int, message: String) {
        Log.d("unityStartUpObserveManager", "Progress 업데이트: $progress%, $message")
        _progressFlow.value = progress
        _messageFlow.value = message
    }

    override fun completeLoading() {
        Log.d("unityStartUpObserveManager", "로딩 완료 처리")
        _completionFlow.tryEmit(Unit)
    }

    override fun onCharacterTypeChanged(characterType: String) {
        val getCharacterType = when(characterType.lowercase()){
            "healmongging", "heal" -> "heal"
            "hpmongging", "physical" -> "physical"
            "jobmongging", "work" -> "work"
            else -> "UNKNOWN"
        }
        Log.d("unityStartUpObserveManager", "캐릭터 타입 변경: $getCharacterType")
        _characterTypeChangeFlow.tryEmit(getCharacterType)
    }

    override fun goToInGame() {
        Log.d("unityStartUpObserveManager", "인게임 이동 처리")
        _goToInGameFlow.tryEmit(Unit)
    }


}