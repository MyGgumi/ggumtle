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
