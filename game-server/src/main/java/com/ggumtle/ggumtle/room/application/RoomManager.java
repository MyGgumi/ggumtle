package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.session.Session;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.Collections;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Slf4j
@Component
public class RoomManager {

    // 방 ID 생성을 위한 시퀀스
    private final AtomicLong roomIdGenerator = new AtomicLong(0);

    // 방 관리를 위한 맵
    // TODO: DB로 변경해도 괜찮을 듯?
    private final ConcurrentHashMap<Long, Room> roomMap = new ConcurrentHashMap<>();

    /**
     * 방을 조회합니다.
     *
     * @param roomId 조회할 방 ID
     * @return 조회된 방
     */
    public Room getRoom(Long roomId) {
        if (!roomMap.containsKey(roomId)) {
            throw new IllegalArgumentException("방 조회에 실패했습니다. roomId: " + roomId);
        }

        log.debug("{} 방 조회", roomId);
        return roomMap.get(roomId);
    }

    /**
     * 새로운 방을 생성합니다.
     *
     * @return 생성된 방 ID
     */
    public Room createRoom() {
        long roomId = roomIdGenerator.incrementAndGet();

        log.debug("{} 방 생성", roomId);

        Room room = new Room(roomId);
        roomMap.put(roomId, room);

        return room;
    }

    /**
     * 방을 삭제합니다.
     *
     * @param roomId 삭제할 방 ID
     */
    public void removeRoom(Long roomId) {
        if (!roomMap.containsKey(roomId)) {
            throw new IllegalArgumentException("방 삭제에 실패했습니다. roomId: " + roomId);
        }

        log.debug("{} 방 삭제", roomId);
        roomMap.remove(roomId);
    }

    /**
     * 세션을 방에 추가합니다.
     *
     * @param roomId  세션을 추가할 방 ID
     * @param session 추가할 세션
     */
    public void addSession(Long roomId, Session session) {
        if (!roomMap.containsKey(roomId)) {
            throw new IllegalArgumentException("방 조회에 실패했습니다. roomId: " + roomId);
        }

        log.debug("{} 방에 세션 추가: {}", roomId, session.getSessionId());
        roomMap.get(roomId).addSession(session);
    }

    /**
     * 모든 방 리스트를 반환합니다.
     *
     * @return 방 리스트
     */
    public List<Room> getRooms() {
        return Collections.unmodifiableList(roomMap.values().stream().toList());
    }

    /**
     * party 멤버 모두가 들어갈 수 있는 방을 조회합니다.
     * 없으면 새로운 방을 생성합니다.
     *
     * @param partyMemberCount 파티 멤버 수
     * @return 조회된 방
     */
    public Room getRoomForPartyMembers(int partyMemberCount) {
        return roomMap.values().stream()
                .filter(room -> room.isAvailable(partyMemberCount))
                .findFirst()
                .orElse(createRoom());
    }
}
