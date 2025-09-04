package com.ggumtle.ggumtle.server.packet;

import com.ggumtle.ggumtle.room.application.command.JoinRoomCommand;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum ReceivePacketType {
    // 인증
    VERIFY_TOKEN((short) 1, null),

    // 방 관리
    ROOM_JOIN((short) 10, JoinRoomCommand.class),
    SCENE_CHANGE((short) 20, null),
    ;

    private final short value;
    private final Class<?> clazz;

    public static ReceivePacketType fromValue(short value) {
        for (ReceivePacketType type : values()) {
            if (type.getValue() == value) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown packet type: " + value);
    }
}
