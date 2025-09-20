package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import com.ggumtle.ggumtle.room.application.dto.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.dto.SceneChangeResult;
import com.ggumtle.ggumtle.room.domain.MonggingStat;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.session.Session;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
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
    private final ConcurrentHashMap<Long, Room> idToRoom = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Long, Room> playerIdToRoom = new ConcurrentHashMap<>();

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

        List<MonggingStat> monggingStats = new ArrayList<>();
        for (RequestRoomMessage.Player player : request.players()) {
            monggingStats.add(
                    MonggingStat.builder()
                            .playerId(player.id())
                            .monggingClassId(player.monggingClassId())
                            .additionalHp(player.additionalHp())
                            .additionalHealSpeed(player.additionalHealSpeed())
                            .additionalTaskSpeed(player.additionalTaskSpeed())
                            .build()
            );
        }
        Room room = new Room(roomId, monggingStats);
        idToRoom.put(roomId, room);

        return room;
    }

    public void insertRoom(Room room) {
        idToRoom.put(room.id, room);
    }

    public void removeRoom(Long roomId) {
        if (!idToRoom.containsKey(roomId)) {
            throw new IllegalArgumentException("방 삭제에 실패했습니다. roomId: " + roomId);
        }

        log.debug("{} 방 삭제", roomId);
        idToRoom.remove(roomId);
    }

    public void removeSession(Session session) {
        for (Room room : idToRoom.values()) {
            room.removeSession(session);
        }
        playerIdToRoom.remove(session.getMemberId());
    }

    public List<Room> getRooms() {
        return idToRoom.values().stream().toList();
    }

    public JoinRoomResult joinRoom(Long roomId, Session session) {
        Room room = idToRoom.getOrDefault(roomId, null);
        if (room == null) {
            log.error("[{}] {}번 방을 찾을 수 없습니다", session.getChannel().id(), roomId);
            return JoinRoomResult.FAIL;
        }

        int connectedSessionCount = room.addSession(session);
        if (connectedSessionCount == -1) {
            return JoinRoomResult.FAIL;
        }

        playerIdToRoom.put(session.getMemberId(), room);
        log.debug("[{}] {}번 방에 세션 추가 결과: 현재 인원 {}인", session.getChannel().id(), roomId, connectedSessionCount);

        return connectedSessionCount >= room.getPlayerSize() ? JoinRoomResult.DONE : JoinRoomResult.SUCCESS;
    }

    public SceneChangeResult changeScene(Session session) {
        Room room = playerIdToRoom.getOrDefault(session.getMemberId(), null);
        if (room == null) {
            log.error("[{}] {}번 사용자에 연결된 방을 찾을 수 없음", session.getChannel().id(), session.getMemberId());
            return new SceneChangeResult(SceneChangeResult.Status.FAIL, null);
        }

        int sceneChangerCount = room.addSceneChanger(session.getMemberId());
        if (sceneChangerCount == -1) {
            return new SceneChangeResult(SceneChangeResult.Status.FAIL, null);
        }

        playerIdToRoom.put(session.getMemberId(), room);
        log.debug("[{}] {}번 방에 씬 체인저 추가 결과: 현재 인원 {}인", session.getChannel().id(), room.id, sceneChangerCount);

        return new SceneChangeResult(
                sceneChangerCount >= room.getPlayerSize() ? SceneChangeResult.Status.DONE : SceneChangeResult.Status.SUCCESS,
                room.id);
    }

    public void broadcast(long roomId, Packet packet) {
        this.idToRoom.get(roomId).broadcast(packet);
    }
}
