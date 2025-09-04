package com.ggumtle.ggumtle.room.domain;

import com.ggumtle.ggumtle.event.DreamStartEvent;
import com.ggumtle.ggumtle.room.application.result.JoinRoomResult;
import com.ggumtle.ggumtle.room.application.result.SceneChangeResult;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.Session;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;

import java.util.List;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArraySet;

@Getter
@Slf4j
public class Room {

    private final long roomId;
    private final Set<Long> playerIds;
    private final ConcurrentHashMap<Long, Session> playerSessions;
    private final CopyOnWriteArraySet<Long> sceneChanger;
    private final ApplicationEventPublisher applicationEventPublisher;

    public Room(long roomId, List<Long> playerIds, ApplicationEventPublisher applicationEventPublisher) {
        this.roomId = roomId;
        this.playerIds = Set.of(playerIds.toArray(new Long[0]));
        this.playerSessions = new ConcurrentHashMap<>(playerIds.size());
        this.sceneChanger = new CopyOnWriteArraySet<>();
        this.applicationEventPublisher = applicationEventPublisher;
    }

    public int getConnectedPlayerCount() {
        return playerSessions.size();
    }

    /**
     * 세션을 추가한다.
     * 세션 추가 후 모두 접속된 경우 브로드캐스팅한다.
     * @param session 추가할 세션
     */
    public void addSession(Session session) {
        if (!playerIds.contains(session.getMemberId())) {
            throw new RuntimeException("입장할 수 없는 방입니다");
        }

        final boolean isFull;
        synchronized (playerSessions) {
            if (playerSessions.containsKey(session.getMemberId())) {
                log.debug("{}번 방에 재입장: {}", this.roomId, session);
            }

            playerSessions.put(session.getMemberId(), session);

            isFull = playerSessions.size() >= playerIds.size();
        }

        JoinRoomResult result = new JoinRoomResult(1);
        Packet packet = Packet.of(SendPacketType.ROOM_JOIN_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);

        if (isFull) {
            Packet donePacket = Packet.of(SendPacketType.ROOM_JOIN_DONE, System.currentTimeMillis(), null);
            broadcast(donePacket);
        }
    }

    public boolean removeSession(Session session) {
        Session removedSession = playerSessions.remove(session.getMemberId());
        if (removedSession == null) {
            return false;
        }

        sceneChanger.remove(removedSession.getMemberId());
        return true;
    }

    /**
     * 씬 체인지가 완료된 사용자를 추가한다.
     * @param playerId 씬 체인지를 완료한 사용자
     * @return
     */
    public boolean addSceneChanger(Long playerId) {
        if (!playerSessions.containsKey(playerId)) {
            log.debug("{}번 방에 {}번 사용자가 연결되지 않아 씬 체인지 기록 불가", this.roomId, playerId);
            return false;
        }

        if (sceneChanger.contains(playerId)) {
            log.debug("{}번 방에 {}번 사용자는 이미 씬 체인지 완료함", this.roomId, playerId);
            return false;
        }

        final boolean isAllChanged;
        synchronized (sceneChanger) {
            sceneChanger.add(playerId);
            log.info("{}번 방에 {}번 사용자 씬 체인지 완료", this.roomId, playerId);

            isAllChanged = sceneChanger.size() >= playerIds.size();
        }

        SceneChangeResult result = new SceneChangeResult(0);
        Packet packet = Packet.of(SendPacketType.SCENE_CHANGE_RESULT, System.currentTimeMillis(), result);
        playerSessions.get(playerId).sendPacket(packet);

        if (isAllChanged) {
            DreamStartEvent dreamStartEvent = new DreamStartEvent(this.roomId);
            applicationEventPublisher.publishEvent(dreamStartEvent);
        }

        return true;
    }

    public void broadcast(Packet packet) {
        playerSessions.values().forEach(session -> session.sendPacket(packet));
    }

    public boolean sendPacket(long memberId, Packet packet) {
        if (!playerSessions.containsKey(memberId)) {
            return false;
        }

        playerSessions.get(memberId).sendPacket(packet);
        return true;
    }
}
