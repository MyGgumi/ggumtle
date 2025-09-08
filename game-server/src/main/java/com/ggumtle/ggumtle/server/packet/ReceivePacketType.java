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
    SCENE_CHANGE((short) 20),

    // 드림 플레이
    PLAYER_MOVE((short) 40),

    // 플레이어 간 상호작용
    HIT_MONGGING((short) 50),

    // 상자 및 아이템 상호작용
    SHOW_BOX((short) 60),
    MOVE_ITEM((short) 62),
    CLOSE_BOX((short) 64),

    // 꿈틀이 상호작용
    DIG_UP_GGUMTLE((short) 100),
    STOP_DIGGING((short) 102),
    START_FEED((short) 110),
    STOP_FEED((short) 112),
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
