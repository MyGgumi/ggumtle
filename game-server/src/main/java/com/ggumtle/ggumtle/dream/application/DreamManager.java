package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.command.MoveItemCommand;
import com.ggumtle.ggumtle.dream.application.result.DigUpReceiveResult;
import com.ggumtle.ggumtle.dream.application.result.DigUpResult;
import com.ggumtle.ggumtle.dream.application.result.FeedDoneResult;
import com.ggumtle.ggumtle.dream.application.result.HitMonggingResult;
import com.ggumtle.ggumtle.dream.application.result.InitializeMapResult;
import com.ggumtle.ggumtle.dream.application.result.InitializePlayerResult;
import com.ggumtle.ggumtle.dream.application.result.MoveItemResult;
import com.ggumtle.ggumtle.dream.application.result.PlayerMoveResult;
import com.ggumtle.ggumtle.dream.application.result.ShowBoxResult;
import com.ggumtle.ggumtle.dream.application.result.StartFeedResult;
import com.ggumtle.ggumtle.dream.application.result.StopDiggingResult;
import com.ggumtle.ggumtle.dream.application.result.StopFeedingResult;
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
import java.util.Arrays;
import java.util.Comparator;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Random;
import java.util.concurrent.ConcurrentLinkedDeque;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

@Slf4j
public class DreamManager {

    private static final int BOX_SPAWN_SIZE = 20;
    private static final int GGUMTLE_SPAWN_SIZE = 3;

    private final ScheduledExecutorService ggumtleWorkerThread;
    private final ConcurrentLinkedDeque<WorkingGgumtleThread> workingGgumtleThreads;

    // 정적 데이터 캐싱
    private final SpawnCache spawnCache;

    // 인게임 캐시
    private final Room room;
    private Map<Long, Player> players;
    private final Map<Integer, Ggumtle> ggumtles;
    private final Map<Integer, Box> boxes;

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

        ggumtleWorkerThread = Executors.newScheduledThreadPool(players.size());
        workingGgumtleThreads = new ConcurrentLinkedDeque<>();
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

    public void showBox(int boxId, Session session) {
        if (!boxes.containsKey(boxId)) {
            ShowBoxResult showBoxResult = new ShowBoxResult(false, -1, null);
            Packet packet = Packet.of(SendPacketType.SHOW_BOX_RESULT, System.currentTimeMillis(), showBoxResult);
            session.sendPacket(packet);
            return;
        }

        Box box = boxes.get(boxId);
        Item[] items = box.getItems();
        box.addViewer(session);

        ShowBoxResult showBoxResult = new ShowBoxResult(true, boxId, items);
        Packet packet = Packet.of(SendPacketType.SHOW_BOX_RESULT, System.currentTimeMillis(), showBoxResult);
        session.sendPacket(packet);
    }

    public void closeBox(int boxId, Session session) {
        Box box = boxes.getOrDefault(boxId, null);

        if (box == null) {
            return;
        }

        box.removeViewer(session);
    }

    public void moveItem(byte directionValue, int boxId, int index, Session session) {
        MoveItemCommand.DIRECTION direction = MoveItemCommand.DIRECTION.valueOf(directionValue);

        // 방향 변수 검사
        if (direction == null) {
            MoveItemResult result = new MoveItemResult(MoveItemResult.MoveResult.NOT_FOUNT_DIR, null);
            Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
        }

        // 인덱스 검사
        if (index < 0
                || (direction == MoveItemCommand.DIRECTION.BOX_TO_INVENTORY && index >= Box.BOX_SIZE)
                || (direction == MoveItemCommand.DIRECTION.INVENTORY_TO_BOX && index >= Mongging.INVENTORY_SIZE)) {
            MoveItemResult result = new MoveItemResult(MoveItemResult.MoveResult.INDEX_OUT_OF_RANGE, null);
            Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 박스 존재 검사
        if (!boxes.containsKey(boxId)) {
            MoveItemResult result = new MoveItemResult(MoveItemResult.MoveResult.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 플레이어 검사
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (player == null) {
            MoveItemResult result = new MoveItemResult(MoveItemResult.MoveResult.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!(player instanceof Mongging mongging)) {
            MoveItemResult result = new MoveItemResult(MoveItemResult.MoveResult.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 아이템 이동
        Box box = boxes.get(boxId);
        Result result;
        if (direction == MoveItemCommand.DIRECTION.BOX_TO_INVENTORY) {
            result = moveItemFromBoxToInventory(index, box, mongging, session);
        } else {
            result = moveItemFromInventoryToBox(index, box, mongging, session);
        }

        if (result == null) {
            return;
        }

        Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
        List<Session> viewers = box.getViewers();
        for (Session viewer : viewers) {
            viewer.sendPacket(packet);
        }
    }

    public void digUpGgumtle(int ggumtleId, Session session) {
        if (!ggumtles.containsKey(ggumtleId)) {
            DigUpReceiveResult result = new DigUpReceiveResult(DigUpReceiveResult.DigUpResult.NOT_FOUND);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Ggumtle ggumtle = ggumtles.get(ggumtleId);
        if (ggumtle.isDugUp()) {
            DigUpReceiveResult result = new DigUpReceiveResult(DigUpReceiveResult.DigUpResult.ALREADY_DIG_UP);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        ScheduledFuture<?> future = ggumtleWorkerThread.schedule(() -> {
            workingGgumtleThreads.removeIf(thread -> {
                boolean isSameWork = thread.ggumtleId == ggumtle.getId() && thread.threadType == WorkingGgumtleThread.ThreadType.DIG_UP;
                if (isSameWork) {
                    thread.scheduledFuture.cancel(true);
                }
                return isSameWork;
            });

            // 3초를 기다리는 동안 누군가 파냈으면 무시
            if (!ggumtle.tryDigUp()) {
                return;
            }

            DigUpResult result = new DigUpResult(ggumtle.getId(), true);
            Packet packet = Packet.of(SendPacketType.DIG_UP_DONE, System.currentTimeMillis(), result);
            this.room.broadcast(packet);
        }, 3, TimeUnit.SECONDS);
        workingGgumtleThreads.add(new WorkingGgumtleThread(session.getMemberId(), ggumtle.getId(), future, WorkingGgumtleThread.ThreadType.DIG_UP));

        DigUpReceiveResult result = new DigUpReceiveResult(DigUpReceiveResult.DigUpResult.START_DIGGING);
        Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void stopDigging(Session session) {
        boolean isStopped = workingGgumtleThreads.removeIf(thread -> {
            boolean target = thread.playerId == session.getMemberId();
            if (target) {
                thread.scheduledFuture.cancel(true);
            }
            return target;
        });

        Result result;
        if (isStopped) {
            result = new StopDiggingResult(StopDiggingResult.StopResult.STOP);
        } else {
            result = new StopDiggingResult(StopDiggingResult.StopResult.NOT_FOUND_DIGGING);
        }
        Packet packet = Packet.of(SendPacketType.STOP_DIGGING, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void startFeed(int ggumtleId, Session session) {
        if (!ggumtles.containsKey(ggumtleId)) {
            StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.NOT_FOUND);
            Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Ggumtle ggumtle = ggumtles.get(ggumtleId);
        if (!ggumtle.isDugUp()) {
            StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.YET_DIG_UP);
            Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (ggumtle.isDone()) {
            StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.ALREADY_DONE);
            Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Mongging mongging = (Mongging) players.get(session.getMemberId());
        int index = mongging.findItemIndex(Item.GGUMTLE_FEED);
        if (index == -1) {
            StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.LACK_OF_FEED_ITEM);
            Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Runnable task = () -> {
            final int left = ggumtle.feed();
            mongging.popItem(index);

            // 남은 아이템이 없으면 종료
            int leftFeedItem = mongging.countItem(Item.GGUMTLE_FEED);
            if (leftFeedItem == 0) {
                Result result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);

                this.workingGgumtleThreads.removeIf(thread -> {
                    boolean target = thread.playerId == session.getMemberId();
                    if (target) {
                        thread.scheduledFuture.cancel(true);
                    }
                    return target;
                });
                return;
            }

            // 성불시키면 종료
            if (left <= 0) {
                this.workingGgumtleThreads.removeIf(thread -> {
                    boolean isSameWork = thread.ggumtleId == ggumtle.getId();
                    if (isSameWork) {
                        thread.scheduledFuture.cancel(true);
                    }
                    return isSameWork;
                });

                Result result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);

                result = new FeedDoneResult(ggumtle.getId());
                packet = Packet.of(SendPacketType.FEED_DONE, System.currentTimeMillis(), result);
                this.room.broadcast(packet);
            }
        };
        ScheduledFuture<?> future = ggumtleWorkerThread.scheduleAtFixedRate(task, 1, 1, TimeUnit.SECONDS);
        workingGgumtleThreads.add(new WorkingGgumtleThread(session.getMemberId(), ggumtle.getId(), future, WorkingGgumtleThread.ThreadType.FEED));

        StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.START_FEEDING);
        Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void stopFeeding(Session session) {
        boolean isStopped = workingGgumtleThreads.removeIf(thread -> {
            boolean target = thread.playerId == session.getMemberId();
            if (target) {
                thread.scheduledFuture.cancel(true);
            }
            return target;
        });

        Mongging mongging = (Mongging) players.get(session.getMemberId());
        int leftItemCount = mongging.countItem(Item.GGUMTLE_FEED);

        Result result;
        if (isStopped) {
            result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, leftItemCount);
        } else {
            result = new StopFeedingResult(StopFeedingResult.StopResult.NOT_FOUND, leftItemCount);
        }
        Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    /**
     * 아이템을 박스에서 인벤토리로 이동한다.
     * 이동에 실패할 경우 주어진 세션에 메시지를 전송하고, 이동에 성공하면 이동 결과를 반환한다.
     */
    private Result moveItemFromBoxToInventory(int index, Box box, Mongging mongging, Session session) {
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));

        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                Item targetItem = box.getItemAt(index);
                if (targetItem == null) {
                    Result result = new MoveItemResult(MoveItemResult.MoveResult.NOT_FOUND_BOX, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                if (!mongging.canAddItem(targetItem)) {
                    Result result = new MoveItemResult(MoveItemResult.MoveResult.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                Item[] popResult = box.popItem(index);
                boolean success = mongging.addItem(popResult[Box.BOX_SIZE]);

                if (!success) {
                    log.error("사용자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패했습니다. 인벤토리: {}",
                            targetItem,
                            popResult[Box.BOX_SIZE],
                            Arrays.deepToString(mongging.getItems()));

                    Result result = new MoveItemResult(MoveItemResult.MoveResult.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                return new MoveItemResult(MoveItemResult.MoveResult.SUCCESS, popResult);
            }
        }
    }

    /**
     * 아이템을 인벤토리에서 상자로 이동한다.
     * 이동에 실패할 경우 주어진 세션에 메시지를 전송하고, 이동에 성공하면 이동 결과를 반환한다.
     */
    private Result moveItemFromInventoryToBox(int index, Box box, Mongging mongging, Session session) {
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));

        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                Item targetItem = mongging.getItemAt(index);
                if (targetItem == null) {
                    Result result = new MoveItemResult(MoveItemResult.MoveResult.NOT_FOUND_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                if (!box.canAddItem(targetItem)) {
                    Result result = new MoveItemResult(MoveItemResult.MoveResult.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                Item popItem = mongging.popItem(index);
                boolean success = box.addItem(popItem);

                if (!success) {
                    log.error("상자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패했습니다. 상자: {}",
                            targetItem,
                            popItem,
                            Arrays.deepToString(mongging.getItems()));

                    Result result = new MoveItemResult(MoveItemResult.MoveResult.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.MOVE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                }

                return new MoveItemResult(MoveItemResult.MoveResult.INDEX_OUT_OF_RANGE, box.getItems());
            }
        }
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
