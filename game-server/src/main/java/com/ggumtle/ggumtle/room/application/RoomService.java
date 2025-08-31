package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.room.command.RoomCreateCommand;
import com.ggumtle.ggumtle.server.PacketCommandHandler;
import com.ggumtle.ggumtle.server.packet.PacketType;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

@Slf4j
@Service
@RequiredArgsConstructor
public class RoomService {

    private final RoomManager roomManager;

    @PacketCommandHandler(type = PacketType.ROOM_GET_OR_CREATE)
    public void getOrCreateRoom(RoomCreateCommand command) {
        log.info("방 생성 요청 - partyMemberCount: {}", command.partyMemberCount());
    }
}
