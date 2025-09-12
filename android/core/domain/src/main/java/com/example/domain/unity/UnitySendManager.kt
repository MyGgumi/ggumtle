package com.ggumtle.domain.unity

import com.ggumtle.domain.unity.model.UnityMessage
import kotlinx.coroutines.flow.SharedFlow

interface UnitySendManager {
    val targetFlow: SharedFlow<String>
    val methodFlow: SharedFlow<String>
    val paramsFlow: SharedFlow<List<Any>>

    fun sendToUnity(target: String, methodName: String, params: List<Any> = emptyList())
    val unityMessageFlow: SharedFlow<UnityMessage>
}