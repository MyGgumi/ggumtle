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

    // 게임 플레이
    PLAYER_MOVE((short) 40),
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
