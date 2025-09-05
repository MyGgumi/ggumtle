package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.result.HitMonggingResult;
import com.ggumtle.ggumtle.dream.application.result.InitializeMapResult;
import com.ggumtle.ggumtle.dream.application.result.InitializePlayerResult;
import com.ggumtle.ggumtle.dream.application.result.PlayerMoveResult;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.Mongdung;
import com.ggumtle.ggumtle.dream.domain.Mongging;
import com.ggumtle.ggumtle.dream.domain.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.dream.util.ItemDistributor;
import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import com.ggumtle.ggumtle.dream.vo.Item;
import com.ggumtle.ggumtle.dream.vo.PlayerSpawn;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.Session;
import lombok.extern.slf4j.Slf4j;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

@Slf4j
public class DreamManager {

    private static final int BOX_SPAWN_SIZE = 20;
    private static final int GGUMTLE_SPAWN_SIZE = 3;

    // 정적 데이터 캐싱
    private final SpawnCache spawnCache;

    // 인게임 캐시
    private final Room room;
    private Map<Long, Player> players;
    private Map<Integer, Ggumtle> ggumtles;
    private Map<Integer, Box> boxes;

    public DreamManager(Room room, SpawnCache spawnCache) {
        log.info("{}번 게임의 초기화 시작", room.getRoomId());

        this.spawnCache = spawnCache;
        this.room = room;
        this.ggumtles = new HashMap<>();
        this.boxes = new HashMap<>();

        log.info("{}번 게임의 초기화 시작", room.getRoomId());

        initializeMap();
        initializePlayers();

        log.info("{}번 게임의 초기화 종료", room.getRoomId());
    }

    public void movePlayer(long id, int x, int y, int z) {
        Position position = new Position(x, y, z, System.currentTimeMillis());

        Player player = players.get(id);
        player.addPosition(position);

        Result result = new PlayerMoveResult(player.getId(), x, y, z);
        Packet packet = Packet.of(SendPacketType.PLAYER_MOVE_RELAY, System.currentTimeMillis(), result);
        this.room.broadcast(packet);
    }

    public void hitMongging(HitMonggingCommand command, Session session, long timestamp) {
        Player requester = players.getOrDefault(session.getMemberId(), null);

        if (requester == null) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.NOT_PLAYER, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!(requester instanceof Mongdung)) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.NOT_MONGDUNG, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Mongging target = (Mongging) players.getOrDefault(command.targetId(), null);
        if (target == null) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.NOT_FOUND_TARGET, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Mongdung mongdung = (Mongdung) requester;
        boolean isHit = mongdung.detectHit(command.vx(), command.vy(), command.vz(), timestamp, target);

        if (!isHit) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.FAIL, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        int damage = mongdung.getDamage();
        int leftHp = target.getHit(damage);

        Result result = new HitMonggingResult(HitMonggingResult.HitResult.SUCCESS, leftHp);
        Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
        room.sendPacket(List.of(mongdung.getId(), target.getId()), packet);
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

    // TODO: 클래스 별 체력, 속도 초기화
    private void initializePlayers() {
        List<Long> playerIds = room.getPlayerIds().stream().toList();
        List<PlayerSpawn> playerSpawns = spawnCache.getRandomPlayerSpawns(playerIds.size());

        int mongdungIndex = pickMongdungIndex(playerIds.size());

        this.players = new HashMap<>();
        for (int i = 0; i < playerIds.size(); i++) {
            if (i == mongdungIndex) {
                Mongdung mongdung = new Mongdung(playerIds.get(i), Position.from(playerSpawns.get(i)));
                this.players.put(mongdung.getId(), mongdung);
                continue;
            }

            Mongging mongging = new Mongging(playerIds.get(i), Position.from(playerSpawns.get(i)));
            this.players.put(mongging.getId(), mongging);
        }

        log.info("{}번 게임의 플레이어 초기화 종료", room.getRoomId());
        log.debug("{}번 게임의 플레이어: {}", room.getRoomId(), this.players.values());

        List<Player> players = this.players.values().stream().toList();
        for (long playerId : playerIds) {
            Result result = new InitializePlayerResult(players, playerId);
            Packet packet = Packet.of(SendPacketType.INITIALIZE_PLAYER, System.currentTimeMillis(), result);
            boolean success = room.sendPacket(playerId, packet);
            if (!success) {
                log.error("{}번 사용자에게 {}번 게임의 플레이어 초기 정보를 전송하지 못했습니다", playerId, this.room.getRoomId());
            }
        }
    }

    private int pickMongdungIndex(int size) {
        Random random = new Random();

        return random.nextInt(size);
    }
}
