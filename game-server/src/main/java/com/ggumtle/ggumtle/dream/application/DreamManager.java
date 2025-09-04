package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.application.result.InitializeMapResult;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.dream.util.ItemDistributor;
import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import lombok.extern.slf4j.Slf4j;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

@Slf4j
public class DreamManager {

    private static final int BOX_SPAWN_SIZE = 20;
    private static final int GGUMTLE_SPAWN_SIZE = 3;

    // 정적 데이터 캐싱
    private final SpawnCache spawnCache;

    // 인게임 캐시
    private final Room room;
    private List<Player> players;
    private ConcurrentHashMap<Integer, Ggumtle> ggumtles;
    private ConcurrentHashMap<Integer, Box> boxes;

    public DreamManager(Room room, SpawnCache spawnCache) {
        log.info("{}번 게임의 초기화 시작", room.getRoomId());

        this.spawnCache = spawnCache;
        this.room = room;
        this.ggumtles = new ConcurrentHashMap<>();
        this.boxes = new ConcurrentHashMap<>();

        initializeMap();
        initializePlayers(room.getPlayerIds().stream().toList());

        log.info("{}번 게임의 초기화 종료", room.getRoomId());
    }

    private void initializeMap() {
        // 꿈틀이 위치 초기화
        List<GgumtleSpawn> ggumtleSpawns = spawnCache.getRandomGgumtleSpawns(GGUMTLE_SPAWN_SIZE);
        for (int i = 0; i < GGUMTLE_SPAWN_SIZE; i++) {
            ggumtles.put(i, new Ggumtle(i, Position.from(ggumtleSpawns.get(i))));
        }

        // 상자 위치 초기화
        List<BoxSpawn> boxSpawns = spawnCache.getRandomBoxSpawns(BOX_SPAWN_SIZE);
        for (int i = 0; i < BOX_SPAWN_SIZE; i++) {
            boxes.put(i, new Box(i, Position.from(boxSpawns.get(i))));
        }

        // 상자 아이템 초기화
        List<Box> boxes = this.boxes.values().stream().toList();
        for (Item item : Item.values()) {
            ItemDistributor.distribute(item, boxes, item.getInitialCount());
        }

        // TODO: 필드템 초기화

        log.info("{}번 게임의 맵 초기화 종료", room.getRoomId());

        Result result = new InitializeMapResult(new ArrayList<>(this.boxes.values()), new ArrayList<>(this.ggumtles.values()), List.of(), List.of());
        Packet packet = Packet.of(SendPacketType.INITIALIZE_MAP, System.currentTimeMillis(), result);
        this.room.broadcast(packet);
    }

    // TODO: 체력, 속도, 아이템, 위치 초기화
    private void initializePlayers(List<Long> playerIds) {
        this.players = new ArrayList<>();

        for (long playerId : room.getPlayerIds()) {
            Player player = new Player(playerId);

            this.players.add(player);
        }

        log.info("{}번 게임의 플레이어 초기화 종료", room.getRoomId());
    }
}
