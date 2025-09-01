package com.ggumtle.ggumtle.server.packet;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum SendPacketType {
    VERIFY_TOKEN_RESULT((short) 2),

    ROOM_JOIN_RESULT((short) 13),  // int result
    SCENE_CHANGE_RESULT((short) 14),
    GAME_START_RESULT((short) 15),
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
