package com.ggumtle.ggumtle.init;

import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.Room;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor
@Slf4j
public class RoomCreator {
    private final RoomManager roomManager;

    @EventListener(ApplicationReadyEvent.class)
    public void createTestRoom() {
        Room room3 = new Room(-1L, List.of(1L, 2L, 3L));
        Room room4 = new Room(-2L, List.of(1L, 2L, 3L, 4L));
        Room room5 = new Room(-3L, List.of(1L, 2L, 3L, 4L, 5L));
        Room monggingRoom = new Room(-4L, List.of(1L));
        Room mongdungRoom = new Room(-5L, List.of(1L));

        roomManager.insertRoom(room3);
        roomManager.insertRoom(room4);
        roomManager.insertRoom(room5);
        roomManager.insertRoom(monggingRoom);
        roomManager.insertRoom(mongdungRoom);
    }
}
