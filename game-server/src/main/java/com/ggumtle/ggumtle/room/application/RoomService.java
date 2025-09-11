package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.common.event.DisconnectSessionEvent;
import com.ggumtle.ggumtle.common.event.DreamStartEvent;
import com.ggumtle.ggumtle.room.application.command.JoinRoomCommand;
import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.room.application.dto.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.dto.SceneChangeResult;
import com.ggumtle.ggumtle.room.application.body.JoinRoomBody;
import com.ggumtle.ggumtle.room.application.body.SceneChangeBody;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.Session;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

@Slf4j
@Service
@RequiredArgsConstructor
public class RoomService {

    private final RoomManager roomManager;
    private final ApplicationEventPublisher applicationEventPublisher;

    @PacketCommandHandler(type = ReceivePacketType.ROOM_JOIN)
    public void joinRoom(JoinRoomCommand command, Session session) {
        log.info("[{}] 방 입장 요청 - Type: {}, Session: {}", session.getChannel().id(), command.roomId(), session);

        JoinRoomResult result = roomManager.joinRoom(command.roomId(), session);

        if (result == JoinRoomResult.FAIL) {
            Body body = new JoinRoomBody(JoinRoomBody.Result.FAIL);
            Packet packet = Packet.of(SendPacketType.ROOM_JOIN, System.currentTimeMillis(), body);
            session.sendPacket(packet);
            return;
        }

        Body body = new JoinRoomBody(JoinRoomBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.ROOM_JOIN, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        if (result == JoinRoomResult.DONE) {
            applicationEventPublisher.publishEvent(new DreamStartEvent(command.roomId()));
        }
    }

    @PacketCommandHandler(type = ReceivePacketType.SCENE_CHANGE)
    public void changeScene(Session session) {
        log.info("[{}] 씬 변경 요청 - Session: {}", session.getChannel().id(), session);

        SceneChangeResult result = roomManager.changeScene(session);

        if (result.status() == SceneChangeResult.Status.FAIL) {
            Body body = new SceneChangeBody(SceneChangeBody.Result.FAIL);
            Packet packet = Packet.of(SendPacketType.SCENE_CHANGE, System.currentTimeMillis(), body);
            session.sendPacket(packet);
            return;
        }

        Body body = new SceneChangeBody(SceneChangeBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.SCENE_CHANGE, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        if (result.status() == SceneChangeResult.Status.DONE) {
            packet = Packet.of(SendPacketType.GAME_START, System.currentTimeMillis(), null);
            roomManager.broadcast(result.roomId(), packet);
        }
    }

    @EventListener
    public void leaveRoom(DisconnectSessionEvent event) {
        this.roomManager.removeSession(event.session());
    }
}
