package com.ggumtle.ggumtle.server.packet;

import com.ggumtle.ggumtle.room.command.RoomCreateCommand;
import com.ggumtle.ggumtle.common.dto.Command;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum ReceivePacketType {
    // 요청
    VERIFY_TOKEN((short) 1, null),

    ROOM_GET_OR_CREATE((short) 10, RoomCreateCommand.class),

    ROOM_JOIN((short) 11, null),
    SCENE_CHANGE((short) 12, null),
    ;

    private final short value;
    private final Class<? extends Command> clazz;

    public static ReceivePacketType fromValue(short value) {
        for (ReceivePacketType type : values()) {
            if (type.getValue() == value) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown packet type: " + value);
    }
}
