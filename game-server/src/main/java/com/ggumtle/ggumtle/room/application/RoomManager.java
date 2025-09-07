package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.common.event.JoinRoomEvent;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.session.Session;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;

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
    private final ApplicationEventPublisher applicationEventPublisher;

    public Optional<Room> getRoomById(Long roomId) {
        Room room = idToRoom.getOrDefault(roomId, null);

        if (room == null) {
            log.warn("{}번 방 조회에 실패했습니다", roomId);
            return  Optional.empty();
        }

        log.debug("{}번 방 조회", room.getRoomId());
        return Optional.of(room);
    }

    public Optional<Room> getRoomByPlayerId(Long playerId) {
        Room room = playerIdToRoom.getOrDefault(playerId, null);

        if (room == null) {
            log.warn("{}번 사용자의 방 조회에 실패했습니다", playerId);
            return  Optional.empty();
        }

        log.debug("{}번 사용자의 방 조회: {}", playerId, room.getRoomId());
        return Optional.of(room);
    }

    /**
     * 새로운 방을 생성합니다.
     *
     * @return 생성된 방 ID
     */
    public Room createRoom(List<Long> players) {
        long roomId = roomIdGenerator.incrementAndGet();

        log.debug("{}번 방 생성: {}", roomId, players);

        Room room = new Room(roomId, players, applicationEventPublisher);
        idToRoom.put(roomId, room);

        return room;
    }

    public void insertRoom(Room room) {
        idToRoom.put(room.getRoomId(), room);
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

    public boolean joinRoom(Long roomId, Session session) {
        Room existingRoom = playerIdToRoom.getOrDefault(session.getMemberId(), null);
        if (existingRoom != null) {
            log.error("{}번 사용자는 이미 {}번 방에 들어와있습니다", session.getMemberId(), existingRoom.getRoomId());
            return false;
        }

        Room room = idToRoom.getOrDefault(roomId, null);
        if (room == null) {
            log.error("{}번 방을 찾을 수 없습니다", roomId);
            return false;
        }

        room.addSession(session);
        playerIdToRoom.put(session.getMemberId(), room);
        log.debug("{}번 방에 세션 추가 결과: 현재 인원 {}인", roomId, idToRoom.get(roomId).getConnectedPlayerCount());

        applicationEventPublisher.publishEvent(new JoinRoomEvent(roomId, session));
        return true;
    }

    /**
     * 모든 방 리스트를 반환합니다.
     *
     * @return 방 리스트
     */
    public List<Room> getRooms() {
        return idToRoom.values().stream().toList();
    }
}
