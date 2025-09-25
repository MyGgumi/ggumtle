package com.ggumtle.domain.unity

import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

interface UnityStartupObserveManager {
    val progressFlow: StateFlow<Int>
    val messageFlow: StateFlow<String>

    val completionFlow: SharedFlow<Unit>
    val characterTypeChangeFlow: SharedFlow<String>
    val goToInGameFlow: SharedFlow<Unit>

    fun updateProgress(progress: Int, message: String)
    fun completeLoading()
    fun onCharacterTypeChanged(characterType: String)

    fun goToInGame()

}