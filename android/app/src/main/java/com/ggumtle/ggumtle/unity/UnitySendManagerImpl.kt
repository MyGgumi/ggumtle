package com.ggumtle.ggumtle.unity

import com.example.domain.unity.UnitySendManager
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnitySendManagerImpl @Inject constructor() : UnitySendManager {

    private val _targetFlow = MutableSharedFlow<String>(replay = 1)
    override val targetFlow: SharedFlow<String> = _targetFlow.asSharedFlow()

    private val _methodFlow = MutableSharedFlow<String>(replay = 1)
    override val methodFlow: SharedFlow<String> = _methodFlow.asSharedFlow()

    private val _paramsFlow = MutableSharedFlow<List<Any>>(replay = 1)
    override val paramsFlow: SharedFlow<List<Any>> = _paramsFlow.asSharedFlow()

    override fun sendToUnity(target: String, methodName: String, params: List<Any>) {
        _targetFlow.tryEmit(target)
        _methodFlow.tryEmit(methodName)
        _paramsFlow.tryEmit(params)
    }
}