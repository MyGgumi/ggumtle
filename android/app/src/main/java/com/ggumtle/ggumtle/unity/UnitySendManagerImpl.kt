package com.ggumtle.ggumtle.unity

import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.domain.websocket.model.UnityMonggingClass
import com.ggumtle.domain.unity.model.UnityMessage
import com.ggumtle.domain.unity.model.UnityMethod
import com.ggumtle.domain.unity.model.UnityTarget
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class UnitySendManagerImpl @Inject constructor() : UnitySendManager {

    private val _unityMessageFlow = MutableSharedFlow<UnityMessage>(replay = 1)
    override val unityMessageFlow: SharedFlow<UnityMessage> = _unityMessageFlow.asSharedFlow()
    override fun goToHomeFromLogin() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.START_TRANSITION.value
        )
    }

    override fun goToLoginFromHome() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.START_REVERSE.value
        )
    }

    override fun goToGrowthFromHome() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.ROTATE_CAMERA_TO_ENHANCE.value
        )
    }

    override fun goToHomeFromGrowth() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.RESET_CAMERA_ROTATION.value
        )
    }

    override fun addMyCharacter(nickname: String, level: Int) {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.SET_FIRST_CHARACTER.value,
            listOf(nickname, level)
        )
    }

    override fun addOthersCharacter(nickname: String, level: Int) {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.ADD_CHARACTER_BY_NICKNAME.value,
            listOf(nickname, level)
        )
    }

    override fun removeTargetCharacter(nickname: String) {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.REMOVE_CHARACTER_BY_NICKNAME.value,
            listOf(nickname)
        )
    }

    override fun changeTargetCharacterType(
        nickname: String,
        characterType: UnityMonggingClass
    ) {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.ADD_CHARACTER_BY_NICKNAME.value,
            listOf(nickname, characterType.type)
        )
    }

    override fun changeGrowthCharacterTypeNext() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.MOVE_TO_NEXT_TYPE.value
        )
    }

    override fun changeGrowthCharacterTypePrevious() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.MOVE_TO_PREVIOUS_TYPE.value
        )
    }

    override fun playEnhanceSuccessEffect() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.PLAY_ENHANCE_SUCCESS_EFFECT.value
        )
    }

    override fun playEnhanceFailEffect() {
        sendToUnity(
            UnityTarget.ANDROID_UNITY_CONTROLLER.value,
            UnityMethod.PLAY_ENHANCE_FAIL_EFFECT.value
        )
    }

    override fun goToInGame(accessToken: String, roomId: Int, host: String, port: Int) {
        sendToUnity(
            UnityTarget.LOBBY_SCENE_MANAGER.value,
            UnityMethod.START_GAME_COMMAND_FROM_ANDROID.value,
            listOf(accessToken, roomId, host, port)
        )
    }

    override fun goToOutGame() {
    }

    override fun sendToUnity(target: String, methodName: String, params: List<Any>) {
        _unityMessageFlow.tryEmit(UnityMessage(target, methodName, params))
    }
}