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

        roomManager.insertRoomOfId(-1L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-2L, playerInfos.subList(0, 4));
        roomManager.insertRoomOfId(-3L, playerInfos.subList(0, 5));
        roomManager.insertRoomOfId(-4L, playerInfos.subList(0, 1));
        roomManager.insertRoomOfId(-5L, playerInfos.subList(0, 1));

        roomManager.insertRoomOfId(-6L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-7L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-8L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-9L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-10L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-11L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-12L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-13L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-14L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-15L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-16L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-17L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-18L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-19L, playerInfos.subList(0, 3));
        roomManager.insertRoomOfId(-20L, playerInfos.subList(0, 3));
    }
}
