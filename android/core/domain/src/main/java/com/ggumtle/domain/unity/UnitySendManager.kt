package com.ggumtle.domain.unity

import com.ggumtle.domain.websocket.model.UnityMonggingClass
import com.ggumtle.domain.unity.model.UnityMessage
import com.ggumtle.domain.websocket.model.PartyMember
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
    fun changeTargetCharacterType(nickname: String, characterType: UnityMonggingClass, level: Int)
    // TODO: 내 캐릭터 레벨만 업데이트하는 메소드 추가 필요
    // fun updateMyCharacterLevel(level: Int)
    fun changeGrowthCharacterTypeNext()
    fun changeGrowthCharacterTypePrevious()
    fun playEnhanceSuccessEffect()
    fun playEnhanceFailEffect()
    fun goToInGame(accessToken: String, roomId: Int, host: String, port: Int)
    fun goToOutGame()

    fun changeTargetCharacterNickname(nickname: String, newNickname: String)
    fun enterNewPartyMember(nickname: String, type: UnityMonggingClass, level: Int)
    fun updateParty(participants: List<PartyMember>, myId: Long?)
}