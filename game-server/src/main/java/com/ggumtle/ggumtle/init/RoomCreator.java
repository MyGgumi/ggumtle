package com.ggumtle.ggumtle.init;

import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.MonggingStat;
import com.ggumtle.ggumtle.room.domain.Room;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;

@Component
@RequiredArgsConstructor
@Slf4j
public class RoomCreator {
    private final RoomManager roomManager;

    @EventListener(ApplicationReadyEvent.class)
    public void createTestRoom() {
        List<MonggingStat> monggingStats = new ArrayList<>();
        for (int i = 1; i <= 5; i++) {
            monggingStats.add(
                    MonggingStat.builder()
                            .playerId(i)
                            .monggingClassId(0)
                            .additionalHp(0)
                            .additionalTaskSpeed(0)
                            .additionalHealSpeed(0)
                            .build()
            );
        }

        Room room3 = new Room(-1L, monggingStats.subList(0, 3));
        Room room4 = new Room(-2L, monggingStats.subList(0, 4));
        Room room5 = new Room(-3L, monggingStats.subList(0, 5));
        Room monggingRoom = new Room(-4L, monggingStats.subList(0, 1));
        Room mongdungRoom = new Room(-5L, monggingStats.subList(0, 1));

        roomManager.insertRoom(room3);
        roomManager.insertRoom(room4);
        roomManager.insertRoom(room5);
        roomManager.insertRoom(monggingRoom);
        roomManager.insertRoom(mongdungRoom);
    }
}
