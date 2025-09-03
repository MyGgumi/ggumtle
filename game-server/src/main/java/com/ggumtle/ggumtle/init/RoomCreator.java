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
        Room room = new Room(-1L, List.of(1L, 2L, 3L));

        roomManager.insertRoom(room);
    }
}
