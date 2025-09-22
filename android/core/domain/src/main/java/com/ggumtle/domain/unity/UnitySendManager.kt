package com.ggumtle.domain.unity

import com.ggumtle.domain.websocket.model.UnityMonggingClass
import com.ggumtle.domain.unity.model.UnityMessage
import kotlinx.coroutines.flow.SharedFlow

interface UnitySendManager {
    fun sendToUnity(target: String, methodName: String, params: List<Any> = emptyList())
    val unityMessageFlow: SharedFlow<UnityMessage>
    fun goToHomeFromLogin()
    fun goToLoginFromHome()
    fun goToGrowthFromHome()
    fun goToHomeFromGrowth()
    fun addMyCharacter(nickname: String, level: Int)
    fun addOthersCharacter(nickname: String, level: Int)
    fun removeTargetCharacter(nickname: String)
    fun changeTargetCharacterType(nickname: String, characterType: UnityMonggingClass)
    fun changeGrowthCharacterTypeNext()
    fun changeGrowthCharacterTypePrevious()
    fun playEnhanceSuccessEffect()
    fun playEnhanceFailEffect()
    fun goToInGame(partyId: String, accessToken: String)
    fun goToOutGame()

}