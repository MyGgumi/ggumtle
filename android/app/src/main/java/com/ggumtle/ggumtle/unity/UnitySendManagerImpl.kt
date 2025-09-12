package com.ggumtle.ggumtle.unity

import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.domain.unity.model.UnityMessage
import com.unity3d.player.UnityPlayer
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnitySendManagerImpl @Inject constructor() : UnitySendManager {

    private val _unityMessageFlow = MutableSharedFlow<UnityMessage>(replay = 1)
    override val unityMessageFlow: SharedFlow<UnityMessage> = _unityMessageFlow.asSharedFlow()

    private val _targetFlow = MutableSharedFlow<String>(replay = 0)
    override val targetFlow: SharedFlow<String> = _targetFlow.asSharedFlow()

    private val _methodFlow = MutableSharedFlow<String>(replay = 0)
    override val methodFlow: SharedFlow<String> = _methodFlow.asSharedFlow()

    private val _paramsFlow = MutableSharedFlow<List<Any>>(replay = 0)
    override val paramsFlow: SharedFlow<List<Any>> = _paramsFlow.asSharedFlow()

    override fun sendToUnity(target: String, methodName: String, params: List<Any>) {
        _unityMessageFlow.tryEmit(UnityMessage(target, methodName, params))
//        // 기존 방식도 유지 (호환성을 위해)
//        _targetFlow.tryEmit(target)
//        _methodFlow.tryEmit(methodName)
//        _paramsFlow.tryEmit(params)
    }
}