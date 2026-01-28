package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.common.event.DisconnectChannelEvent;
import com.ggumtle.ggumtle.common.event.DreamEndEvent;
import com.ggumtle.ggumtle.common.event.StartDreamEvent;
import com.ggumtle.ggumtle.common.annotation.RequestPacketHandler;
import com.ggumtle.ggumtle.dream.application.tickevent.TickEvent;
import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import com.ggumtle.ggumtle.room.application.body.CreateRoomBody;
import com.ggumtle.ggumtle.room.application.body.JoinRoomBody;
import com.ggumtle.ggumtle.room.application.body.SceneChangeBody;
import com.ggumtle.ggumtle.room.application.dto.CreateRoomCommand;
import com.ggumtle.ggumtle.room.application.request.CreateRoomRequest;
import com.ggumtle.ggumtle.room.application.request.JoinRoomRequest;
import com.ggumtle.ggumtle.room.application.dto.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.dto.SceneChangeResult;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.application.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.tick.TickWorkerPool;
import io.netty.channel.Channel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.Optional;

@Slf4j
@Service
@RequiredArgsConstructor
public class RoomService {

    private final RoomManager roomManager;
    private final TickWorkerPool tickWorkerPool;
    private final ApplicationEventPublisher applicationEventPublisher;

    @RequestPacketHandler(type = ReceivePacketType.ROOM_JOIN)
    public void joinRoom(JoinRoomRequest request, Channel channel) {
        log.info("[{}] 방 입장 요청 - Type: {}", channel.id(), request.roomId());

        JoinRoomResult result = roomManager.joinRoom(request.roomId(), channel);

        if (result == JoinRoomResult.FAIL) {
            Body body = new JoinRoomBody(JoinRoomBody.Result.FAIL);
            Packet packet = Packet.of(SendPacketType.ROOM_JOIN, System.currentTimeMillis(), body);
            channel.writeAndFlush(packet);
            return;
        }

        Body body = new JoinRoomBody(JoinRoomBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.ROOM_JOIN, System.currentTimeMillis(), body);
        channel.writeAndFlush(packet);

        if (result == JoinRoomResult.DONE) {
            roomManager.broadcastInitialDream(request.roomId());
        }
    }

    @RequestPacketHandler(type = ReceivePacketType.ROOM_CREATE)
    public void createRoom(CreateRoomRequest request, Channel channel) {
        log.info("[{}] 방 생성 요청 - 플레이어 수: {}", channel.id(), request.players().size());

        // 요청자가 플레이어 목록에 포함되어 있는지 확인
        boolean creatorInList = request.players().stream()
                .anyMatch(p -> p.playerId() == ChannelManager.getMemberId(channel));
        if (!creatorInList) {
            log.warn("[{}] 방 생성 실패: 요청자가 플레이어 목록에 없음", channel.id());
            Body body = new CreateRoomBody(CreateRoomBody.Result.FAIL, 0L);
            Packet packet = Packet.of(SendPacketType.ROOM_CREATE, System.currentTimeMillis(), body);
            channel.writeAndFlush(packet);
            return;
        }

        // 방 생성
        CreateRoomCommand command = request.toCommand();
        Room room = roomManager.createTestRoom(command);
        tickWorkerPool.assignRoom(room);

        Body body = new CreateRoomBody(CreateRoomBody.Result.SUCCESS, room.id);
        Packet packet = Packet.of(SendPacketType.ROOM_CREATE, System.currentTimeMillis(), body);
        channel.writeAndFlush(packet);
    }

    public Room createRoom(RequestRoomMessage request) {
        CreateRoomCommand command = request.toCommand();
        Room room = roomManager.createRoom(command);
        tickWorkerPool.assignRoom(room);
        return room;
    }

    @RequestPacketHandler(type = ReceivePacketType.SCENE_CHANGE)
    public void changeScene(Channel channel) {
        log.info("[{}] 씬 변경 요청", channel.id());

        SceneChangeResult result = roomManager.changeScene(channel);

        if (result.status() == SceneChangeResult.Status.FAIL) {
            Body body = new SceneChangeBody(SceneChangeBody.Result.FAIL);
            Packet packet = Packet.of(SendPacketType.SCENE_CHANGE, System.currentTimeMillis(), body);
            channel.writeAndFlush(packet);
            return;
        }

        Body body = new SceneChangeBody(SceneChangeBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.SCENE_CHANGE, System.currentTimeMillis(), body);
        channel.writeAndFlush(packet);

        if (result.status() == SceneChangeResult.Status.DONE) {
            applicationEventPublisher.publishEvent(new StartDreamEvent(result.roomId()));
        }
    }

    @EventListener
    public void leaveRoom(DisconnectChannelEvent event) {
        this.roomManager.leftRoom(event.channel());
    }

    @EventListener
    public void startDream(StartDreamEvent event) {
        this.roomManager.startDream(event.roomId());
    }

    @EventListener
    public void endDream(DreamEndEvent event) {
        Optional<Room> optionalRoom = this.roomManager.getRoomById(event.roomId());
        if (optionalRoom.isEmpty()) {
            log.error("드림 종료 실패: {}번 방이 없습니다", event.roomId());
            return;
        }

        Room room = optionalRoom.get();

        tickWorkerPool.unassignRoom(room);

        List<Channel> playerChannels = room.getPlayerChannels();
        playerChannels.forEach(channel -> {
            this.roomManager.leftRoom(channel);
        });
        this.roomManager.removeRoom(room.id);
    }

    public void dispatchToRoom(TickEvent tickEvent, Channel channel) {
        Long memberId = ChannelManager.getMemberId(channel);
        Optional<Room> optionalRoom = this.roomManager.getRoomByPlayerId(memberId);

        if (optionalRoom.isEmpty()) {
            log.error("틱 이벤트 디스패치 실패: {}번 사용자에 해당하는 방이 없음", memberId);
            return;
        }

        optionalRoom.get().addTickEvent(tickEvent);
    }
}
