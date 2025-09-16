package com.example.domain.unity

import com.ggumtle.domain.unity.model.UnityMessage
import kotlinx.coroutines.flow.SharedFlow

interface UnitySendManager {
    fun sendToUnity(target: String, methodName: String, params: List<Any> = emptyList())
    val unityMessageFlow: SharedFlow<UnityMessage>
}