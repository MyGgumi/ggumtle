package com.ggumtle.ggumtle.init;

import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;
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
        List<PlayerInfo> playerInfos = new ArrayList<>();
        for (int i = 1; i <= 5; i++) {
            playerInfos.add(
                    PlayerInfo.builder()
                            .playerId(i)
                            .nickname("플레이어" + i)
                            .monggingClassId(0)
                            .additionalHp(0)
                            .additionalTaskSpeed(0)
                            .additionalHealSpeed(0)
                            .build()
            );
        }

        Room room3 = new Room(-1L, playerInfos.subList(0, 3));
        Room room4 = new Room(-2L, playerInfos.subList(0, 4));
        Room room5 = new Room(-3L, playerInfos.subList(0, 5));
        Room monggingRoom = new Room(-4L, playerInfos.subList(0, 1));
        Room mongdungRoom = new Room(-5L, playerInfos.subList(0, 1));

        roomManager.insertRoom(room3);
        roomManager.insertRoom(room4);
        roomManager.insertRoom(room5);
        roomManager.insertRoom(monggingRoom);
        roomManager.insertRoom(mongdungRoom);
    }
}
