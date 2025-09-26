package com.ggumtle.ggumtle.server.packet;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum SendPacketType {
    // 인증
    VERIFY_TOKEN((short) 2),

    // 방 관리
    ROOM_JOIN((short) 11),

    // 드림 초기화
    INITIALIZE_MAP((short) 20),
    INITIALIZE_PLAYER((short) 21),
    SCENE_CHANGE((short) 31),
    GAME_START((short) 35),

    // 드림 플레이
    PLAYER_MOVE_RELAY((short) 41),
    MONGGING_STATUS((short) 42),
    NEW_GGUMTLE((short) 43),

    // 플레이어 간 상호작용
    HIT((short) 61),
    START_REVIVE((short) 63),
    DONE_REVIVE((short) 64),
    STOP_REVIVE((short) 66),
    MONGDUNG_SKILL((short) 68),
    ATTACK_WITH_ITEM((short) 70),
    USE_FIELD_ITEM((short) 72),
    USE_DEFIBRILLATOR((short) 74),

    // 상자 상호작용
    SHOW_BOX((short) 51),
    CLOSE_BOX((short) 53),
    TAKE_ITEM((short) 55),
    PUT_ITEM((short) 57),

    // 꿈틀이 상호작용
    DIG_UP_RECEIVE((short) 101),
    STOP_DIGGING((short) 103),
    START_FEED((short) 111),
    STOP_FEED((short) 113),
    GGUMTLE_STATUS((short) 120),
    LEFT_JELLY_COUNT((short) 121),
    GGUMTLE_FED_JELLY((short) 122),

    // 탈출
    OPEN_EXIT((short) 130),
    ESCAPE_RESULT((short) 141),

    // 드림 종료
    END((short) 200)
    ;

    private final short value;

    public static SendPacketType fromValue(short value) {
        for (SendPacketType type : values()) {
            if (type.getValue() == value) {
                return type;
            }
        }

        throw new IllegalArgumentException("Unknown packet type: " + value);
    }
}
