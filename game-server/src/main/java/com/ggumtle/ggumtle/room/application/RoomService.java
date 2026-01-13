package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.common.event.DisconnectSessionEvent;
import com.ggumtle.ggumtle.common.event.CreateDreamEvent;
import com.ggumtle.ggumtle.common.event.StartDreamEvent;
import com.ggumtle.ggumtle.room.application.command.CreateRoomCommand;
import com.ggumtle.ggumtle.room.application.command.JoinRoomCommand;
import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.room.application.body.CreateRoomBody;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;
import com.ggumtle.ggumtle.room.domain.Room;

import java.util.List;
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
            applicationEventPublisher.publishEvent(new CreateDreamEvent(command.roomId()));
        }
    }

    @PacketCommandHandler(type = ReceivePacketType.ROOM_CREATE)
    public void createRoom(CreateRoomCommand command, Session session) {
        log.info("[{}] 방 생성 요청 - 플레이어 수: {}, Session: {}",
                session.getChannel().id(), command.players().size(), session);

        // 요청자가 플레이어 목록에 포함되어 있는지 확인
        boolean creatorInList = command.players().stream()
                .anyMatch(p -> p.playerId() == session.getMemberId());

        if (!creatorInList) {
            log.warn("[{}] 방 생성 실패: 요청자가 플레이어 목록에 없음", session.getChannel().id());
            Body body = new CreateRoomBody(CreateRoomBody.Result.FAIL, 0L);
            Packet packet = Packet.of(SendPacketType.ROOM_CREATE, System.currentTimeMillis(), body);
            session.sendPacket(packet);
            return;
        }

        // PlayerInfo 리스트 생성
        List<PlayerInfo> playerInfos = command.players().stream()
                .map(p -> PlayerInfo.builder()
                        .playerId(p.playerId())
                        .nickname("LoadTest-" + p.playerId())
                        .monggingClassId(p.monggingClassId())
                        .additionalHp(p.additionalHp())
                        .additionalHealSpeed(p.additionalHealSpeed())
                        .additionalTaskSpeed(p.additionalTaskSpeed())
                        .build())
                .toList();

        // 방 생성
        Room room = roomManager.createRoomFromPacket(playerInfos);
        log.info("[{}] 방 생성 완료 - roomId: {}", session.getChannel().id(), room.id);

        Body body = new CreateRoomBody(CreateRoomBody.Result.SUCCESS, room.id);
        Packet packet = Packet.of(SendPacketType.ROOM_CREATE, System.currentTimeMillis(), body);
        session.sendPacket(packet);
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
            long now = System.currentTimeMillis();
            packet = Packet.of(SendPacketType.GAME_START, now, null);
            roomManager.broadcast(result.roomId(), packet);
            applicationEventPublisher.publishEvent(new StartDreamEvent(result.roomId(), now));
        }
    }

    @EventListener
    public void leaveRoom(DisconnectSessionEvent event) {
        this.roomManager.removeSession(event.session());
    }
}
