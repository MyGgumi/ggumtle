package com.ggumtle.ggumtle.server.packet;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum ReceivePacketType {
    // 인증
    VERIFY_TOKEN((short) 1),

    // 방 관리
    ROOM_JOIN((short) 10),

    // 게임 초기화
    SCENE_CHANGE((short) 30),

    // 게임 운영
    PLAYER_MOVE((short) 40),

    // 플레이어 간 상호작용
    HIT_MONGGING((short) 60),
    START_REVIVE((short) 62),
    STOP_REVIVE((short) 65),
    MONGDUNG_SKILL((short) 67),
    ATTACK_WITH_ITEM((short) 69),
    USE_FIELD_ITEM((short) 71),

    // 상자 및 아이템 상호작용
    SHOW_BOX((short) 50),
    CLOSE_BOX((short) 52),
    TAKE_ITEM_FROM_BOX((short) 54),
    PUT_ITEM_TO_BOX((short) 56),

    // 꿈틀이 상호작용
    DIG_UP_GGUMTLE((short) 100),
    STOP_DIGGING((short) 102),
    START_FEED((short) 110),
    STOP_FEED((short) 112),

    // 탈출
    ESCAPE((short) 140),
    ;

    private final short value;

    public static ReceivePacketType fromValue(short value) {
        for (ReceivePacketType type : values()) {
            if (type.getValue() == value) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown packet type: " + value);
    }
}
