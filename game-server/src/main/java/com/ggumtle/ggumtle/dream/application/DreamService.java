package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.event.DreamStartEvent;
import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.Room;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class DreamService {

    private final RoomManager roomManager;
    private final ConcurrentHashMap<Long, DreamManager> dreamManagers = new ConcurrentHashMap<>();
    private final SpawnCache spawnCache;

    @EventListener
    public void startDream(DreamStartEvent event) {
        Optional<Room> optionalRoom = roomManager.getRoomById(event.roomId());

        if (optionalRoom.isEmpty()) {
            log.error("{}번 방이 없어 게임을 시작하지 못했습니다", event.roomId());
            return;
        }

        Room room = optionalRoom.get();
        DreamManager dreamManager = new DreamManager(room, spawnCache);

        dreamManagers.put(room.getRoomId(),  dreamManager);
    }
}
