package com.ggumtle.ggumtle.room.domain;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.session.Session;
import lombok.extern.slf4j.Slf4j;

import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CopyOnWriteArraySet;

@Slf4j
public class Room {

    public final long id;
    private final ConcurrentHashMap<Long, PlayerInfo> playerInfos;
    private final ConcurrentHashMap<Long, Session> playerSessions;
    private final CopyOnWriteArraySet<Long> sceneChanger;

    public Room(long id, List<PlayerInfo> playerInfos) {
        this.id = id;
        this.playerInfos = new ConcurrentHashMap<>(playerInfos.size());
        for (PlayerInfo playerInfo : playerInfos) {
            this.playerInfos.put(playerInfo.playerId, playerInfo);
        }

        this.playerSessions = new ConcurrentHashMap<>(playerInfos.size());
        this.sceneChanger = new CopyOnWriteArraySet<>();
    }

    public int getConnectedPlayerCount() {
        return playerSessions.size();
    }

    public List<Long> getPlayerIds() {
        return playerInfos.keySet().stream().toList();
    }

    public PlayerInfo getPlayerInfo(long id) {
        return playerInfos.getOrDefault(id, null);
    }

    public Map<Long, PlayerInfo> getPlayerInfos() {
        return Map.copyOf(this.playerInfos);
    }

    public int getPlayerSize() {
        return playerInfos.size();
    }

    public synchronized List<Session> getPlayerSessions() {
        return List.copyOf(playerSessions.values());
    }


    public int addSession(Session session) {
        if (!playerInfos.containsKey(session.getMemberId())) {
            log.error("[{}] {}번 사용자는 {}번 방에 들어올 수 없습니다", session.getChannel().id(), session.getMemberId(), this.id);
            return -1;
        }

        final int connectedSessionCount;
        synchronized (playerSessions) {
            if (playerSessions.containsKey(session.getMemberId())) {
                log.debug(
                        "[{}] {}번 방의 {}번 사용자의 세션 업데이트: {} -> {}",
                        session.getChannel().id(), id, session.getMemberId(), playerSessions.get(session.getMemberId()), session);
            }

            playerSessions.put(session.getMemberId(), session);
            connectedSessionCount = playerSessions.size();
        }

        return connectedSessionCount;
    }

    public boolean removeSession(Session session) {
        Session removedSession = playerSessions.remove(session.getMemberId());

        return removedSession == null;
    }

    public int addSceneChanger(long playerId) {
        if (!playerSessions.containsKey(playerId)) {
            log.debug("{}번 방에 {}번 사용자가 연결되지 않아 씬 체인지 기록 불가", this.id, playerId);
            return -1;
        }

        if (sceneChanger.contains(playerId)) {
            log.debug("{}번 방에 {}번 사용자는 이미 씬 체인지 완료함", this.id, playerId);
            return 0;
        }

        int sceneChangerCount;
        synchronized (sceneChanger) {
            sceneChanger.add(playerId);
            log.info("{}번 방에 {}번 사용자 씬 체인지 완료", this.id, playerId);

            sceneChangerCount = sceneChanger.size();
        }

        return sceneChangerCount;
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

    public int sendPacket(List<Long> memberIds, Packet packet) {
        int count = 0;

        for (Long memberId : memberIds) {
            Session session = playerSessions.getOrDefault(memberId, null);
            if (session == null) {
                log.warn("{}번 사용자의 세션이 없어 {} 패킷을 전송하지 못했습니다", memberId, packet);
                continue;
            }
            session.sendPacket(packet);
            count++;
        }

        return count;
    }
}
