package com.ggumtle.ggumtle.room.application;

import com.ggumtle.ggumtle.room.application.dto.CreateRoomCommand;
import com.ggumtle.ggumtle.tick.TickThreadPool;
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
public class TestRoomCreator {
    private final RoomManager roomManager;
    private final TickThreadPool tickThreadPool;

    @EventListener(ApplicationReadyEvent.class)
    public void createTestRoom() {
        List<CreateRoomCommand.Player> playerInfos = new ArrayList<>();
        for (int i = 1; i <= 5; i++) {
            playerInfos.add(
                    CreateRoomCommand.Player.builder()
                            .playerId(i)
                            .nickname("플레이어" + i)
                            .monggingClassId(0)
                            .additionalHp(0)
                            .additionalTaskSpeed(0)
                            .additionalHealSpeed(0)
                            .build()
            );
        }

        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -1L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -2L, playerInfos.subList(0, 4))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -3L, playerInfos.subList(0, 5))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -4L, playerInfos.subList(0, 1))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -5L, playerInfos.subList(0, 1))));

        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -6L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -7L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -8L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -9L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -10L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -11L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -12L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -13L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -14L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -15L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -16L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -17L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -18L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -19L, playerInfos.subList(0, 3))));
        tickThreadPool.assignRoom(roomManager.createTestRoom(new CreateRoomCommand( -20L, playerInfos.subList(0, 3))));
    }
}
