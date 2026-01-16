package com.ggumtle.ggumtle.room.domain;

import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.dream.application.tickevent.TickEvent;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import io.netty.channel.Channel;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;

import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.CopyOnWriteArraySet;

@Slf4j
public class Room {

    public final long id;

    private final ConcurrentHashMap<Long, PlayerInfo> playerInfos;
    private final ConcurrentHashMap<Long, Channel> playerChannels;
    private final CopyOnWriteArraySet<Long> sceneChanger;

    private final Dream dream;
    private final ConcurrentLinkedQueue<TickEvent> tickEvents;

    public Room(long id, List<PlayerInfo> playerInfos, ApplicationEventPublisher applicationEventPublisher, SpawnCache spawnCache) {
        this.id = id;
        this.playerInfos = new ConcurrentHashMap<>(playerInfos.size());
        for (PlayerInfo playerInfo : playerInfos) {
            this.playerInfos.put(playerInfo.playerId, playerInfo);
        }

        this.tickEvents = new ConcurrentLinkedQueue<>();

        this.playerChannels = new ConcurrentHashMap<>(playerInfos.size());
        this.sceneChanger = new CopyOnWriteArraySet<>();

        this.dream = new Dream(this, spawnCache, applicationEventPublisher);
    }

    public void addTickEvent(TickEvent tickEvent) {
        this.tickEvents.add(tickEvent);

        log.info("TickEvent: {}", this.tickEvents);
    }

    public void tick() {
        TickEvent e;
        while ((e = tickEvents.poll()) != null) {
            e.process(this.dream);
        }
    }

    public void startDream() {
        long now = System.currentTimeMillis();

        this.dream.setTimer(now);

        Packet packet = Packet.of(SendPacketType.GAME_START, now, null);
        broadcast(packet);
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

    public synchronized List<Channel> getPlayerChannels() {
        return List.copyOf(playerChannels.values());
    }

    public int addChannel(Channel channel) {
        Long memberId = ChannelManager.getMemberId(channel);

        if (!playerInfos.containsKey(ChannelManager.getMemberId(channel))) {
            log.error("[{}] {}번 사용자는 {}번 방에 들어올 수 없습니다", channel.id(), memberId, this.id);
            return -1;
        }

        final int connectedSessionCount;
        synchronized (playerChannels) {
            if (playerChannels.containsKey(memberId)) {
                log.debug(
                        "[{}] {}번 방의 {}번 사용자의 채널 업데이트: {} -> {}",
                        channel.id(), id, memberId, playerChannels.get(memberId).id(), channel.id()
                );
            }

            playerChannels.put(memberId, channel);
            connectedSessionCount = playerChannels.size();
        }

        return connectedSessionCount;
    }

    public void removeChannel(Channel channel) {
        playerChannels.remove(ChannelManager.getMemberId(channel));
        channel.disconnect();
    }

    public int addSceneChanger(long playerId) {
        if (!playerChannels.containsKey(playerId)) {
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
        playerChannels.forEach((id, channel) -> channel.write(packet));
    }

    public void flush() {
        playerChannels.forEach((id, channel) -> channel.flush());
    }
}
