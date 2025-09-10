package com.ggumtle.ggumtle.server.packet;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum SendPacketType {
    // 인증
    VERIFY_TOKEN_RESULT((short) 2),

    // 방 관리
    ROOM_JOIN_RESULT((short) 11),
    ROOM_JOIN_DONE((short) 12),
    SCENE_CHANGE_RESULT((short) 21),

    // 게임 초기화
    INITIALIZE_MAP((short) 30),
    INITIALIZE_PLAYER((short) 31),

    // 게임 플레이
    PLAYER_MOVE_RELAY((short) 41),
    MONGGING_STATUS((short) 42),
    NEW_GGUMTLE((short) 43),

    // 플레이어 간 상호작용
    HIT_RESULT((short) 61),
    START_REVIVE_RESULT((short) 63),
    DONE_REVIVE_RESULT((short) 64),
    STOP_REVIVE_RESULT((short) 66),
    MONGDUNG_SKILL_RESULT((short) 68),

    // 상자 상호작용
    SHOW_BOX_RESULT((short) 51),
    CLOSE_BOX_RESULT((short) 53),
    TAKE_ITEM_RESULT((short) 55),
    PUT_ITEM_RESULT((short) 57),

    // 꿈틀이 상호작용
    DIG_UP_RECEIVE((short) 101),
    STOP_DIGGING((short) 103),
    DIG_UP_DONE((short) 104),
    START_FEED_RESULT((short) 111),
    STOP_FEED_RESULT((short) 113),
    FEED_DONE((short) 120),

    // 탈출
    OPEN_EXIT((short) 130),
    ESCAPE_RESULT((short) 141),

    // 게임 종료
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
