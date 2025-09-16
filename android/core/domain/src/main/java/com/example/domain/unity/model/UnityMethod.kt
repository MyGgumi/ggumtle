package com.ggumtle.domain.unity.model

enum class UnityMethod(val value: String) {

    // ANDROID_UNITY_CONTROLLER

    // StartTransition() : 로그인 -> 대기방
    START_TRANSITION("StartTransition"),

    // StartReverse() : 대기방 -> 로그인
    START_REVERSE("StartReverse"),

    // SetFirstCharacter(String nickname, int level) : 대기방에 최초(자신) 캐릭터 추가(기존의 캐릭터는 지움)
    SET_FIRST_CHARACTER("SetFirstCharacter"),

    // AddCharacterByNickname(String nickname, int level) : 대기방에 캐릭터 추가
    ADD_CHARACTER_BY_NICKNAME("AddCharacterByNickname"),

    // RemoveCharacterByNickname(String nickname) : 대기방에 닉네임에 해당하는 캐릭터 삭제
    REMOVE_CHARACTER_BY_NICKNAME("RemoveCharacterByNickname"),

    // ChangeCharacterType(string nickname, string characterType) : 해당 닉네임의 캐릭터 타입을 해당 타입으로 변경
    CHANGE_CHARACTER_TYPE("ChangeCharacterType"),

    // RotateCameraToEnhance() : 대기방 -> 강화
    ROTATE_CAMERA_TO_ENHANCE("RotateCameraToEnhance"),

    // ResetCameraRotation() : 강화 -> 대기방
    RESET_CAMERA_ROTATION("ResetCameraRotation"),

    // MoveToNextType() : 성장 화면 캐릭터 다음 타입으로 변경 HP -> Job -> Heal
    MOVE_TO_NEXT_TYPE("MoveToNextType"),

    // MoveToPreviousType() : 성장 화면 캐릭터 이전 타입으로 변경 HP -> Job -> Heal
    MOVE_TO_PREVIOUS_TYPE("MoveToPreviousType"),

    // PlayEnhanceSuccessEffect() : 강화 성공 이펙트 실행 (6.5초)
    PLAY_ENHANCE_SUCCESS_EFFECT("PlayEnhanceSuccessEffect"),

    // PlayEnhanceFailEffect() : 강화 실패 이펙트 실행 (3.5초)
    PLAY_ENHANCE_FAIL_EFFECT("PlayEnhanceFailEffect"),

    // MAIN_SCENE_MANAGER

    // GoToInGameWithData(String partyId, String accessToken) : 인게임으로 전환, partyid 엑세스토큰 넘김
    GO_TO_IN_GAME_WITH_DATA("GoToInGameWithData"),

    // IN_GAME_SCENE_MANAGER

    // ExitToOutGame() : 인게임에서 대기방으로 전환(타임라인 실행됨으로 delay 필요)
    EXIT_TO_OUT_GAME("ExitToOutGame"),

}