package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.command.PlayerMoveCommand;
import com.ggumtle.ggumtle.dream.application.command.ShowBoxCommand;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.event.DreamStartEvent;
import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.session.Session;
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
    private final ConcurrentHashMap<Long, DreamManager> sessionIdToDreamManagers = new ConcurrentHashMap<>();
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

        dreamManagers.put(room.getRoomId(), dreamManager);
        room.getPlayerSessions()
                .forEach((sessionId, session) -> sessionIdToDreamManagers.put(sessionId, dreamManager));
        log.debug(sessionIdToDreamManagers.toString());
    }

    @PacketCommandHandler(type = ReceivePacketType.PLAYER_MOVE)
    public void handlePlayerMove(PlayerMoveCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.movePlayer(session.getMemberId(), command.x(), command.y(), command.z());
    }

    @PacketCommandHandler(type = ReceivePacketType.HIT_MONGGING)
    public void handleHitMongging(HitMonggingCommand command, Session session, Timestamp timestamp) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.hitMongging(command, session, timestamp.value);
    }

    @PacketCommandHandler(type = ReceivePacketType.SHOW_BOX)
    public void handleShowBox(ShowBoxCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.showBox(command.boxId(), session);
    }

    private DreamManager getDreamManager(Session session) {
        DreamManager dreamManager = sessionIdToDreamManagers.getOrDefault(session.getSessionId(), null);

        if (dreamManager != null) {
            return dreamManager;
        }

        log.error("{}번 사용자의 {}번 세션의 플레이 중인 드림이 없습니다", session.getMemberId(), session.getSessionId());

        throw new RuntimeException(String.format("%d번 사용자의 %d번 세션의 플레이 중인 드림이 없습니다", session.getMemberId(), session.getSessionId()));
    }
}
