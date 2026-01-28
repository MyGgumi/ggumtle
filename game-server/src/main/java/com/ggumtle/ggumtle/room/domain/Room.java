package com.ggumtle.ggumtle.room.domain;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.application.Dream;
import com.ggumtle.ggumtle.dream.application.body.InitializeMapBody;
import com.ggumtle.ggumtle.dream.application.body.InitializePlayerBody;
import com.ggumtle.ggumtle.dream.application.result.DreamState;
import com.ggumtle.ggumtle.dream.application.tickevent.TickEvent;
import com.ggumtle.ggumtle.dream.domain.player.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.server.application.ChannelManager;
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

        log.trace("TickEvent 추가: {}", this.tickEvents);
    }

    public void tick() {
        TickEvent e;
        while ((e = tickEvents.poll()) != null) {
            e.process(this.dream);
            log.trace("TickEvent 처리: {}", e);
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
            log.error("[{}] 채널 추가 실패: {}번 사용자는 {}번 방에 들어올 수 없습니다", channel.id(), memberId, this.id);
            return -1;
        }

        final int connectedSessionCount;
        synchronized (playerChannels) {
            Channel prevChannel = playerChannels.put(memberId, channel);
            connectedSessionCount = playerChannels.size();

            if (prevChannel == null) {
                log.debug("[{}] 채널 추가 성공: {}번 방의 {}번 사용자의 채널이 추가됨", channel.id(), this.id, memberId);
            } else {
                log.debug(
                        "[{}] 채널 교체: {}번 방의 {}번 사용자의 채널이 변경됨: {} -> {}",
                        channel.id(), this.id, memberId, prevChannel.id(), channel.id()
                );
            }
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

    public void broadcastInitialDream() {
        DreamState dreamState = dream.getDreamState();

        for (Player player : dreamState.players()) {
            Body body = new InitializePlayerBody(player.getId(), dreamState.players(), playerInfos);
            Packet packet = Packet.of(SendPacketType.INITIALIZE_PLAYER, System.currentTimeMillis(), body);
            playerChannels.get(player.getId()).writeAndFlush(packet);
        }


        Body body = new InitializeMapBody(dreamState.boxes(), dreamState.ggumtles(), dreamState.healPacks(), dreamState.speedPacks());
        Packet packet = Packet.of(SendPacketType.INITIALIZE_MAP, System.currentTimeMillis(), body);
        this.broadcast(packet);
    }

    public void broadcast(Packet packet) {
        playerChannels.forEach((id, channel) -> channel.write(packet));
    }

    public void sendPacket(long playerId, Packet packet) {
        Channel channel = playerChannels.get(playerId);
        if (channel != null) {
            channel.write(packet);
        }
    }

    public void flush() {
        playerChannels.forEach((id, channel) -> channel.flush());
    }
}
