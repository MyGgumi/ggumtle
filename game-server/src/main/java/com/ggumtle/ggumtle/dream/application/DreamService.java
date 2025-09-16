package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.common.event.DisconnectSessionEvent;
import com.ggumtle.ggumtle.common.event.JoinRoomEvent;
import com.ggumtle.ggumtle.dream.application.command.CloseBoxCommand;
import com.ggumtle.ggumtle.dream.application.command.DigUpCommand;
import com.ggumtle.ggumtle.dream.application.command.EscapeCommand;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.command.MongdungSkillCommand;
import com.ggumtle.ggumtle.dream.application.command.PutItemCommand;
import com.ggumtle.ggumtle.dream.application.command.StartReviveCommand;
import com.ggumtle.ggumtle.dream.application.command.TakeItemCommand;
import com.ggumtle.ggumtle.dream.application.command.PlayerMoveCommand;
import com.ggumtle.ggumtle.dream.application.command.ShowBoxCommand;
import com.ggumtle.ggumtle.dream.application.command.StartFeedCommand;
import com.ggumtle.ggumtle.dream.application.command.AttackWithItemCommand;
import com.ggumtle.ggumtle.dream.application.command.UseFieldItemCommand;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.common.event.DreamStartEvent;
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

        dreamManagers.put(room.id, dreamManager);
        room.getPlayerSessions()
                .forEach(session -> sessionIdToDreamManagers.put(session.getSessionId(), dreamManager));
        log.debug(sessionIdToDreamManagers.toString());
    }

    @EventListener
    public void joinRoom(JoinRoomEvent event) {
        DreamManager dreamManager = dreamManagers.getOrDefault(event.roomId(), null);

        if (dreamManager == null) {
            log.info("[{}] {}번 방에 입장했지만 방에 해당하는 Dream Manager가 없어 Dream에 등록되지 않았습니다", event.session().getChannel().id(), event.roomId());
            return;
        }

        sessionIdToDreamManagers.put(event.session().getSessionId(), dreamManager);
    }

    @EventListener
    public void leaveRoom(DisconnectSessionEvent event) {
        sessionIdToDreamManagers.remove(event.session().getSessionId());
    }

    @PacketCommandHandler(type = ReceivePacketType.PLAYER_MOVE)
    public void handlePlayerMove(PlayerMoveCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.movePlayer(session, command.x(), command.y(), command.z());
    }

    @PacketCommandHandler(type = ReceivePacketType.HIT_MONGGING)
    public void handleHitMongging(HitMonggingCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.hitMongging(command, session);
    }

    @PacketCommandHandler(type = ReceivePacketType.START_REVIVE)
    public void handleStartRevive(StartReviveCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.startRevive(command.targetMonggingId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.STOP_REVIVE)
    public void handleStopRevive(Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.stopRevive(session);
    }

    @PacketCommandHandler(type = ReceivePacketType.MONGDUNG_SKILL)
    public void handleMongdungSkill(MongdungSkillCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.doSkill(command.skillTypeId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.ATTACK_WITH_ITEM)
    public void handleAttackByItem(AttackWithItemCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.attackWithItem(command, session);
    }

    @PacketCommandHandler(type = ReceivePacketType.ATTACK_WITH_ITEM)
    public void handleUseFieldItem(UseFieldItemCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.useFieldItem(command.itemId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.SHOW_BOX)
    public void handleShowBox(ShowBoxCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.showBox(command.boxId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.TAKE_ITEM_FROM_BOX)
    public void handleTakeItem(TakeItemCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.takeItem(command.boxId(), command.index(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.PUT_ITEM_TO_BOX)
    public void handlePutItem(PutItemCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.putItem(command.itemId(), command.boxId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.CLOSE_BOX)
    public void handleCloseBox(CloseBoxCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.closeBox(command.boxId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.DIG_UP_GGUMTLE)
    public void handleDigUpGgumtle(DigUpCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.digUpGgumtle(command.ggumtleId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.STOP_DIGGING)
    public void handleStopDigging(Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.stopDigging(session);
    }

    @PacketCommandHandler(type = ReceivePacketType.START_FEED)
    public void handleStartFeed(StartFeedCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.startFeed(command.ggumtleId(), session);
    }

    @PacketCommandHandler(type = ReceivePacketType.STOP_FEED)
    public void handleStopFeed(Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.stopFeeding(session);
    }

    @PacketCommandHandler(type = ReceivePacketType.STOP_FEED)
    public void handleEscape(EscapeCommand command, Session session) {
        DreamManager dreamManager = getDreamManager(session);

        dreamManager.escape(command.exitId(), session);
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
