package com.ggumtle.ggumtle.unity

import com.example.domain.unity.UnitySendManager
import com.ggumtle.domain.unity.model.UnityMessage
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnitySendManagerImpl @Inject constructor() : UnitySendManager {

    private val _unityMessageFlow = MutableSharedFlow<UnityMessage>(replay = 1)
    override val unityMessageFlow: SharedFlow<UnityMessage> = _unityMessageFlow.asSharedFlow()

    override fun sendToUnity(target: String, methodName: String, params: List<Any>) {
        _unityMessageFlow.tryEmit(UnityMessage(target, methodName, params))
    }
}