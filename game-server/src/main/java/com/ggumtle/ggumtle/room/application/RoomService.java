package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.common.event.DisconnectSessionEvent;
import com.ggumtle.ggumtle.room.application.command.JoinRoomCommand;
import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.room.application.result.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.result.SceneChangeResult;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.Session;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Slf4j
@Service
@RequiredArgsConstructor
public class RoomService {

    private final RoomManager roomManager;

    @PacketCommandHandler(type = ReceivePacketType.ROOM_JOIN)
    public void joinRoom(JoinRoomCommand command, Session session) {
        log.info("[{}] 방 입장 요청 - Type: {}, Session: {}", session.getChannel().id(), command.roomId(), session);

        boolean success = roomManager.joinRoom(command.roomId(), session);

        if (!success) {
            JoinRoomResult result = new JoinRoomResult(0);
            Packet packet = Packet.of(SendPacketType.ROOM_JOIN_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
        }
    }

    @PacketCommandHandler(type = ReceivePacketType.SCENE_CHANGE)
    public void changeScene(Session session) {
        log.info("[{}] 씬 변경 요청 - Session: {}", session.getChannel().id(), session);

        Optional<Room> optionalRoom = roomManager.getRoomByPlayerId(session.getMemberId());
        if (optionalRoom.isEmpty()) {
            log.error("[{}] {}번 사용자가 속한 방을 찾을 수 없어 씬 변경에 실패", session.getChannel().id(), session.getMemberId());

            SceneChangeResult result = new SceneChangeResult(0);
            Packet packet = Packet.of(SendPacketType.SCENE_CHANGE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);

            return;
        }

        Room room = optionalRoom.get();

        boolean success = room.addSceneChanger(session.getMemberId());

        if (!success) {
            SceneChangeResult result = new SceneChangeResult(0);
            Packet packet = Packet.of(SendPacketType.SCENE_CHANGE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
        }
    }

    @EventListener
    public void leaveRoom(DisconnectSessionEvent event) {
        this.roomManager.removeSession(event.session());
    }
}
