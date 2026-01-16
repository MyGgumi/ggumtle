package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import com.ggumtle.ggumtle.room.application.dto.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.dto.SceneChangeResult;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import io.netty.channel.Channel;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Slf4j
@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class RoomManager {

    private final AtomicLong roomIdGenerator = new AtomicLong(0);
    // RoomCreator에서 -1 ~ -20 사용하므로 -100부터 시작하여 ID 충돌 방지
    private final AtomicLong testRoomIdGenerator = new AtomicLong(-100);
    private final ConcurrentHashMap<Long, Room> idToRoom = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Long, Room> playerIdToRoom = new ConcurrentHashMap<>();

    private final ApplicationEventPublisher applicationEventPublisher;
    private final SpawnCache spawnCache;

    public Optional<Room> getRoomById(Long roomId) {
        Room room = idToRoom.getOrDefault(roomId, null);

        if (room == null) {
            log.warn("{}번 방 조회에 실패했습니다", roomId);
            return  Optional.empty();
        }

        log.debug("{}번 방 조회", room.id);
        return Optional.of(room);
    }

    public Optional<Room> getRoomByPlayerId(Long playerId) {
        Room room = playerIdToRoom.getOrDefault(playerId, null);

        if (room == null) {
            log.warn("{}번 사용자의 방 조회에 실패했습니다", playerId);
            return  Optional.empty();
        }

        log.debug("{}번 사용자의 방 조회: {}", playerId, room.id);
        return Optional.of(room);
    }

    public Room createRoom(RequestRoomMessage request) {
        long roomId = roomIdGenerator.incrementAndGet();

        log.debug("{}번 방 생성: {}", roomId, request);

        List<PlayerInfo> playerInfos = new ArrayList<>();
        for (RequestRoomMessage.Player player : request.players()) {
            playerInfos.add(
                    PlayerInfo.builder()
                            .playerId(player.id())
                            .nickname(player.nickname())
                            .monggingClassId(player.monggingClassId())
                            .additionalHp(player.additionalHp())
                            .additionalHealSpeed(player.additionalHealSpeed())
                            .additionalTaskSpeed(player.additionalTaskSpeed())
                            .build()
            );
        }
        Room room = new Room(roomId, playerInfos, applicationEventPublisher, spawnCache);
        idToRoom.put(roomId, room);

        return room;
    }

    public Room createRoomFromPacket(List<PlayerInfo> playerInfos) {
        long roomId = testRoomIdGenerator.decrementAndGet();  // 음수 ID: -101, -102, -103, ...
        log.debug("{}번 방 생성 (패킷): {} 명의 플레이어", roomId, playerInfos.size());

        Room room = new Room(roomId, playerInfos, applicationEventPublisher, spawnCache);
        idToRoom.put(roomId, room);

        return room;
    }

    public void insertRoomOfId(Long id, List<PlayerInfo> playerInfos) {
        idToRoom.put(id, new Room(id, playerInfos, applicationEventPublisher, spawnCache));
    }

    public void removeRoom(Long roomId) {
        if (!idToRoom.containsKey(roomId)) {
            throw new IllegalArgumentException("방 삭제에 실패했습니다. roomId: " + roomId);
        }

        log.debug("{} 방 삭제", roomId);
        idToRoom.remove(roomId);
    }

    public JoinRoomResult joinRoom(Long roomId, Channel channel) {
        Room room = idToRoom.getOrDefault(roomId, null);
        if (room == null) {
            log.error("[{}] {}번 방을 찾을 수 없습니다", channel.id(), roomId);
            return JoinRoomResult.FAIL;
        }

        int connectedChannelCount = room.addChannel(channel);
        if (connectedChannelCount == -1) {
            return JoinRoomResult.FAIL;
        }

        playerIdToRoom.put(ChannelManager.getMemberId(channel), room);
        log.debug("[{}] {}번 방에 세션 추가 결과: 현재 인원 {}인", channel.id(), roomId, connectedChannelCount);

        return connectedChannelCount >= room.getPlayerSize() ? JoinRoomResult.DONE : JoinRoomResult.SUCCESS;
    }

    public void broadcastInitialDream(Long roomId) {
        Room room = idToRoom.getOrDefault(roomId, null);
        if (room == null) {
            log.error("{}번 방을 찾을 수 없습니다", roomId);
            return;
        }

        room.broadcastInitialDream();
    }

    public SceneChangeResult changeScene(Channel channel) {
        Long memberId = ChannelManager.getMemberId(channel);

        Room room = playerIdToRoom.getOrDefault(memberId, null);
        if (room == null) {
            log.error("[{}] {}번 사용자에 연결된 방을 찾을 수 없음", channel.id(), memberId);
            return new SceneChangeResult(SceneChangeResult.Status.FAIL, null);
        }

        int sceneChangerCount = room.addSceneChanger(memberId);
        if (sceneChangerCount == -1) {
            return new SceneChangeResult(SceneChangeResult.Status.FAIL, null);
        }

        playerIdToRoom.put(memberId, room);
        log.debug("[{}] {}번 방에 씬 체인저 추가 결과: 현재 인원 {}인", channel.id(), room.id, sceneChangerCount);

        return new SceneChangeResult(
                sceneChangerCount >= room.getPlayerSize() ? SceneChangeResult.Status.DONE : SceneChangeResult.Status.SUCCESS,
                room.id);
    }

    public void leftRoom(Channel channel) {
        Room room = playerIdToRoom.getOrDefault(ChannelManager.getMemberId(channel), null);

        if (room == null) {
            log.warn("[{}] 방 나가기 요청을 처리할 방이 없음", channel.id());
            return;
        }

        room.removeChannel(channel);

        log.debug("[{}] {}번 방 나가기 완료", channel.id(), room.id);
    }

    public void broadcast(long roomId, Packet packet) {
        this.idToRoom.get(roomId).broadcast(packet);
    }

    public void startDream(long roomId) {
        Room room = idToRoom.getOrDefault(roomId, null);
        if (room == null) {
            log.warn("드림 시작 실패: %d번 방이 없습니다", roomId);
            return;
        }

        room.startDream();
    }

}
