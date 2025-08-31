package com.ggumtle.ggumtle.server.packet;

import com.ggumtle.ggumtle.room.command.RoomCreateCommand;
import com.ggumtle.ggumtle.server.Command;
import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum PacketType {
    // 요청
    ROOM_GET_OR_CREATE((short) 10, RoomCreateCommand.class),
    ;

    private final short value;
    private final Class<? extends Command> clazz;

    public static PacketType fromValue(short value) {
        for (PacketType type : values()) {
            if (type.getValue() == value) {
                return type;
            }
        }
        throw new IllegalArgumentException("Unknown packet type: " + value);
    }
}
