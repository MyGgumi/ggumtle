package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.result.DigUpReceiveResult;
import com.ggumtle.ggumtle.dream.application.result.DigUpResult;
import com.ggumtle.ggumtle.dream.application.result.DoneReviveResult;
import com.ggumtle.ggumtle.dream.application.result.DreamEndResult;
import com.ggumtle.ggumtle.dream.application.result.ExitOpen;
import com.ggumtle.ggumtle.dream.application.result.EscapeResult;
import com.ggumtle.ggumtle.dream.application.result.FeedDoneResult;
import com.ggumtle.ggumtle.dream.application.result.HitMonggingResult;
import com.ggumtle.ggumtle.dream.application.result.InitializeMapResult;
import com.ggumtle.ggumtle.dream.application.result.InitializePlayerResult;
import com.ggumtle.ggumtle.dream.application.result.MongdungSkillResult;
import com.ggumtle.ggumtle.dream.application.result.MonggingStatusResult;
import com.ggumtle.ggumtle.dream.application.result.NewGgumtleResult;
import com.ggumtle.ggumtle.dream.application.result.PutItemResult;
import com.ggumtle.ggumtle.dream.application.result.StartReviveResult;
import com.ggumtle.ggumtle.dream.application.result.StopReviveResult;
import com.ggumtle.ggumtle.dream.application.result.TakeItemResult;
import com.ggumtle.ggumtle.dream.application.result.PlayerMoveResult;
import com.ggumtle.ggumtle.dream.application.result.ShowBoxResult;
import com.ggumtle.ggumtle.dream.application.result.StartFeedResult;
import com.ggumtle.ggumtle.dream.application.result.StopDiggingResult;
import com.ggumtle.ggumtle.dream.application.result.StopFeedingResult;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Exit;
import com.ggumtle.ggumtle.dream.domain.FakeGgumtle;
import com.ggumtle.ggumtle.dream.domain.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.Mongdung;
import com.ggumtle.ggumtle.dream.domain.Mongging;
import com.ggumtle.ggumtle.dream.domain.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.dream.util.ItemDistributor;
import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.ExitSpawn;
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
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Random;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;

@Slf4j
public class DreamManager {

    private static final int BOX_SPAWN_SIZE = 20;
    private static final int GGUMTLE_SPAWN_SIZE = 3;
    private static final int WINNING_MONGGING_COUNT = 2;

    private final ScheduledExecutorService workerThreadPool;
    private final ConcurrentHashMap<Long, WorkingThread> workingThreads;

    // 정적 데이터 캐싱
    private final SpawnCache spawnCache;

    // 인게임 캐시
    private final Room room;
    private final Map<Long, Player> players;
    private final Set<Long> escapedMonggings;
    private final ConcurrentHashMap<Integer, Ggumtle> ggumtles;
    private final AtomicInteger ggumtleIdGenerator;
    private final Map<Integer, Box> boxes;
    private final Map<Integer, Exit> exits;
    private final AtomicBoolean isExitOpen;

    public DreamManager(Room room, SpawnCache spawnCache) {
        log.info("{}번 게임의 초기화 시작", room.getRoomId());

        this.spawnCache = spawnCache;
        this.room = room;
        this.players = new HashMap<>();
        this.escapedMonggings = ConcurrentHashMap.newKeySet();
        this.ggumtles = new ConcurrentHashMap<>();
        this.ggumtleIdGenerator = new AtomicInteger(0);
        this.boxes = new HashMap<>();
        this.exits = new HashMap<>();
        this.isExitOpen = new AtomicBoolean(false);

        log.info("{}번 게임의 초기화 시작", room.getRoomId());

        initializeMap();
        initializePlayers();

        log.info("{}번 게임의 초기화 종료", room.getRoomId());

        this.workerThreadPool = Executors.newScheduledThreadPool(players.size());
        this.workingThreads = new ConcurrentHashMap<>();
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

        if (!(requester instanceof Mongdung mongdung)) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.NOT_MONGDUNG, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Mongging targetMongging = (Mongging) players.getOrDefault(command.targetId(), null);
        if (targetMongging == null) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.NOT_FOUND_TARGET, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        boolean isHit = mongdung.detectHit(command.vx(), command.vy(), command.vz(), timestamp, targetMongging);

        if (!isHit) {
            Result result = new HitMonggingResult(HitMonggingResult.HitResult.FAIL, -1);
            Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        int damage = mongdung.getDamage();
        int leftHp = targetMongging.getHit(damage);

        Result result = new HitMonggingResult(HitMonggingResult.HitResult.SUCCESS, leftHp);
        Packet packet = Packet.of(SendPacketType.HIT_RESULT, System.currentTimeMillis(), result);
        room.sendPacket(List.of(mongdung.getId(), targetMongging.getId()), packet);

        if (leftHp == 0) {
            result = new MonggingStatusResult(targetMongging.getId(), targetMongging.isDead() ? MonggingStatusResult.MonggingStatus.DEAD : MonggingStatusResult.MonggingStatus.KNOCKOUT);
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), result);
            this.room.broadcast(packet);

            WorkingThread removedThread = workingThreads.remove(session.getMemberId());
            if (removedThread != null) {
                removedThread.scheduledFuture.cancel(true);
            }

            distributeDroppedItem(targetMongging);
        }
    }

    public void startRevive(long targetMonggingId, Session session) {
        Player player = players.getOrDefault(session.getMemberId(), null);
        Player targetPlayer = players.getOrDefault(targetMonggingId, null);
        if (player == null || targetPlayer == null) {
            Result result = new StartReviveResult(StartReviveResult.Status.NOT_FOUND_PLAYER);
            Packet packet = Packet.of(SendPacketType.START_REVIVE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!(targetPlayer instanceof Mongging targetMongging) || !(player instanceof Mongging)) {
            Result result = new StartReviveResult(StartReviveResult.Status.NOT_MONGGING);
            Packet packet = Packet.of(SendPacketType.START_REVIVE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!targetMongging.isKnockout()) {
            Result result = new StartReviveResult(StartReviveResult.Status.NOT_KNOCKOUT);
            Packet packet = Packet.of(SendPacketType.START_REVIVE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        ScheduledFuture<?> future = workerThreadPool.schedule(
                () -> {
                    targetMongging.revive();

                    Result result = new DoneReviveResult(targetMongging.getId());
                    Packet packet = Packet.of(SendPacketType.DONE_REVIVE_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);

                    result = new MonggingStatusResult(targetMongging.getId(), MonggingStatusResult.MonggingStatus.NORMAL);
                    packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), result);
                    this.room.broadcast(packet);

                    workingThreads.remove(session.getMemberId());
                }, 3, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.REVIVE, targetMongging.getId()));

        Result result = new StartReviveResult(StartReviveResult.Status.SUCCESS);
        Packet packet = Packet.of(SendPacketType.START_REVIVE_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void stopRevive(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Result result;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.REVIVE) {
            workingThreads.remove(session.getMemberId());
            result = new StopReviveResult(StopReviveResult.Status.SUCCESS);
        } else {
            result = new StopReviveResult(StopReviveResult.Status.FAIL);
        }
        Packet packet = Packet.of(SendPacketType.STOP_REVIVE_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void doSkill(int skillTypeId, Session session) {
        Mongdung.SkillType skillType = Mongdung.SkillType.valueById(skillTypeId);
        if (skillType == null) {
            Result result = new MongdungSkillResult(skillTypeId, MongdungSkillResult.Status.NOT_FOUND_SKILL);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Player player = players.getOrDefault(session.getMemberId(), null);
        if (!(player instanceof Mongdung mongdung)) {
            Result result = new MongdungSkillResult(skillTypeId, MongdungSkillResult.Status.NOT_FOUND_MONGDUNG);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (skillType == Mongdung.SkillType.SCARE) {
            makeScare(mongdung, session);
            return;
        }

        if (skillType == Mongdung.SkillType.FAKE_GGUMTLE) {
            buryFakeGgumtle(mongdung, session);
            return;
        }
    }

    public void makeScare(Mongdung mongdung, Session session) {
        boolean success = mongdung.scare();

        if (!success) {
            Result result = new MongdungSkillResult(Mongdung.SkillType.SCARE, MongdungSkillResult.Status.YET_COOL_TIME);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Result result = new MongdungSkillResult(Mongdung.SkillType.SCARE, MongdungSkillResult.Status.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
        this.room.broadcast(packet);
    }

    public void buryFakeGgumtle(Mongdung mongdung, Session session) {
        Position position = mongdung.getPositionAt(System.currentTimeMillis());

        boolean success = mongdung.tryBuryFakeGgumtle();

        if (!success) {
            Result result = new MongdungSkillResult(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillResult.Status.LACK_USE_COUNT);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Result result = new MongdungSkillResult(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillResult.Status.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);

        int id = ggumtleIdGenerator.addAndGet(1);
        FakeGgumtle fakeGgumtle = new FakeGgumtle(id, position);

        this.ggumtles.put(id, fakeGgumtle);

        result = new NewGgumtleResult(fakeGgumtle.getId(), fakeGgumtle.getPosition());
        packet = Packet.of(SendPacketType.NEW_GGUMTLE, System.currentTimeMillis(), result);
        this.room.broadcast(packet);
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

    public void takeItem(int boxId, int index, Session session) {
        // 인덱스 검사
        if (index < 0 || index >= Box.BOX_SIZE) {
            TakeItemResult result = new TakeItemResult(TakeItemResult.TakeResult.INDEX_OUT_OF_RANGE, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 박스 존재 검사
        Box box = boxes.getOrDefault(boxId, null);
        if (box == null) {
            TakeItemResult result = new TakeItemResult(TakeItemResult.TakeResult.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 플레이어 검사
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (player == null) {
            TakeItemResult result = new TakeItemResult(TakeItemResult.TakeResult.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!(player instanceof Mongging mongging)) {
            TakeItemResult result = new TakeItemResult(TakeItemResult.TakeResult.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 아이템 이동
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));

        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                Item targetItem = box.getItemAt(index);
                if (targetItem == null) {
                    Result result = new TakeItemResult(TakeItemResult.TakeResult.NOT_FOUND_BOX, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                if (!mongging.canAddItem(targetItem)) {
                    Result result = new TakeItemResult(TakeItemResult.TakeResult.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                Item[] popResult = box.popItem(index);
                boolean success = mongging.addItem(popResult[Box.BOX_SIZE]);

                if (!success) {
                    log.error("사용자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패했습니다. 인벤토리: {}",
                            targetItem,
                            popResult[Box.BOX_SIZE],
                            Arrays.deepToString(mongging.getItems()));

                    Result result = new TakeItemResult(TakeItemResult.TakeResult.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                Result result = new TakeItemResult(TakeItemResult.TakeResult.SUCCESS, popResult);
                Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);
            }
        }
    }

    public void putItem(int itemId, int boxId, Session session) {
        Item targetItem = Item.valueOf(itemId);
        if (targetItem == null) {
            Result result = new PutItemResult(PutItemResult.PutResult.ILLEGAL_ITEM_ID, null);
            Packet packet = Packet.of(SendPacketType.PUT_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 박스 존재 검사
        Box box = boxes.getOrDefault(boxId, null);
        if (box == null) {
            Result result = new PutItemResult(PutItemResult.PutResult.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // 플레이어 검사
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (player == null) {
            Result result = new PutItemResult(PutItemResult.PutResult.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        if (!(player instanceof Mongging mongging)) {
            Result result = new PutItemResult(PutItemResult.PutResult.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));
        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                int count = mongging.countItem(targetItem);
                if (count == 0) {
                    Result result = new PutItemResult(PutItemResult.PutResult.NOT_FOUND_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                if (!box.canAddItem(targetItem)) {
                    Result result = new PutItemResult(PutItemResult.PutResult.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                Item poppedItem = mongging.popItem(targetItem);
                boolean success = box.addItem(poppedItem);

                if (!success) {
                    log.error("상자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패했습니다. 상자: {}",
                            targetItem,
                            poppedItem,
                            Arrays.deepToString(mongging.getItems()));

                    Result result = new PutItemResult(PutItemResult.PutResult.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM_RESULT, System.currentTimeMillis(), result);
                    session.sendPacket(packet);
                    return;
                }

                Result result = new PutItemResult(PutItemResult.PutResult.SUCCESS, box.getItems());
                Packet packet = Packet.of(SendPacketType.PUT_ITEM_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);
                return;
            }
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

        ScheduledFuture<?> future = workerThreadPool.schedule(() -> {
            Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
            while (iterator.hasNext()) {
                Map.Entry<Long, WorkingThread> entry = iterator.next();
                if (entry.getValue().threadType == WorkingThread.ThreadType.DIG_UP && entry.getValue().ggumtleId == ggumtle.getId()) {
                    entry.getValue().scheduledFuture.cancel(true);
                    iterator.remove();
                }
            }

            int digUpResult = ggumtle.tryDigUp();

            // 3초를 기다리는 동안 누군가 파냈으면 무시
            if (digUpResult == 0) {
                return;
            }

            DigUpResult result = new DigUpResult(ggumtle.getId(), digUpResult == 1);
            Packet packet = Packet.of(SendPacketType.DIG_UP_DONE, System.currentTimeMillis(), result);
            this.room.broadcast(packet);

            // TODO: 스턴 상태 브로드캐스팅
        }, 3, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.DIG_UP, ggumtle.getId()));

        DigUpReceiveResult result = new DigUpReceiveResult(DigUpReceiveResult.DigUpResult.START_DIGGING);
        Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void stopDigging(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Result result;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.DIG_UP) {
            result = new StopDiggingResult(StopDiggingResult.StopResult.STOP);
            workingThreads.remove(session.getMemberId());
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
        int count = mongging.countItem(Item.GGUMTLE_FEED);
        if (count == 0) {
            StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.LACK_OF_FEED_ITEM);
            Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Runnable task = () -> {
            final int left = ggumtle.feed();
            mongging.popItem(Item.GGUMTLE_FEED);

            // 성불시키면 종료
            if (left <= 0) {
                Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
                while (iterator.hasNext()) {
                    Map.Entry<Long, WorkingThread> entry = iterator.next();
                    if (entry.getValue().ggumtleId == ggumtle.getId()) {
                        entry.getValue().scheduledFuture.cancel(true);
                        iterator.remove();
                    }
                }

                Result result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);

                result = new FeedDoneResult(ggumtle.getId());
                packet = Packet.of(SendPacketType.FEED_DONE, System.currentTimeMillis(), result);
                this.room.broadcast(packet);

                tryOpenExit();

                return;
            }

            // 남은 아이템이 없으면 종료
            int leftFeedItem = mongging.countItem(Item.GGUMTLE_FEED);
            if (leftFeedItem == 0) {
                Result result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
                session.sendPacket(packet);

                WorkingThread removedThread = workingThreads.remove(session.getMemberId());
                removedThread.scheduledFuture.cancel(true);
                return;
            }
        };
        ScheduledFuture<?> future = workerThreadPool.scheduleAtFixedRate(task, 1, 1, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.FEED, ggumtle.getId()));

        StartFeedResult result = new StartFeedResult(StartFeedResult.FeedResult.START_FEEDING);
        Packet packet = Packet.of(SendPacketType.START_FEED_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void stopFeeding(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Mongging mongging = (Mongging) players.get(session.getMemberId());
        int leftItemCount = mongging.countItem(Item.GGUMTLE_FEED);

        Result result;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.FEED) {
            result = new StopFeedingResult(StopFeedingResult.StopResult.STOP, leftItemCount);
            workingThreads.remove(session.getMemberId());
        } else {
            result = new StopFeedingResult(StopFeedingResult.StopResult.NOT_FOUND, leftItemCount);
        }
        Packet packet = Packet.of(SendPacketType.STOP_FEED_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);
    }

    public void tryOpenExit() {
        for (Ggumtle ggumtle : ggumtles.values()) {
            if (!ggumtle.isDone()) {
                return;
            }
        }

        boolean isExpected = isExitOpen.compareAndSet(false, true);
        if (!isExpected) {
            log.warn("{}번 게임의 탈출구 오픈이 다시 이루어졌습니다", this.room.getRoomId());
            return;
        }

        Result result = new ExitOpen(this.exits.values().stream().toList());
        Packet packet = Packet.of(SendPacketType.OPEN_EXIT, System.currentTimeMillis(), result);
        this.room.broadcast(packet);
    }

    public void escape(int exitId, Session session) {
        Exit exit = exits.getOrDefault(exitId, null);

        if (exit == null) {
            Result result = new EscapeResult(EscapeResult.EscapeStatus.NOT_FOUND_EXIT);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        Player player = players.get(session.getMemberId());
        if (!(player instanceof Mongging mongging)) {
            Result result = new EscapeResult(EscapeResult.EscapeStatus.NOT_MONGGING);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        // TODO: 위치 검사

        if (!mongging.isNotDead()) {
            Result result = new EscapeResult(EscapeResult.EscapeStatus.NOT_ALIVE);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), result);
            session.sendPacket(packet);
            return;
        }

        mongging.escape();
        escapedMonggings.add(mongging.getId());

        Result result = new EscapeResult(EscapeResult.EscapeStatus.SUCCESS);
        Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), result);
        session.sendPacket(packet);

        result = new MonggingStatusResult(mongging.getId(), MonggingStatusResult.MonggingStatus.ESCAPE);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), result);
        this.room.broadcast(packet);

        if (escapedMonggings.size() >= WINNING_MONGGING_COUNT) {
            result = new DreamEndResult(DreamEndResult.DreamEndStatus.MONGGING_WIN, escapedMonggings, players.values());
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), result);
            this.room.broadcast(packet);
        }
    }

    private void distributeDroppedItem(Mongging mongging) {
        Map<Item, Integer> droppedItems = mongging.getDroppedItems();
        for (Map.Entry<Item, Integer> entry : droppedItems.entrySet()) {
            ItemDistributor.distribute(entry.getKey(), boxes.values().stream().toList(), entry.getValue());
        }
    }

    private void initializeMap() {
        // 꿈틀이 위치 초기화
        List<GgumtleSpawn> ggumtleSpawns = spawnCache.getRandomGgumtleSpawns(GGUMTLE_SPAWN_SIZE);
        for (int i = 0; i < GGUMTLE_SPAWN_SIZE; i++) {
            ggumtles.put(i, new Ggumtle(i, Position.from(ggumtleSpawns.get(i))));
        }
        ggumtleIdGenerator.set(GGUMTLE_SPAWN_SIZE);

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

        List<ExitSpawn> exitSpawns = spawnCache.getRandomExitSpawns();
        for (int i = 0; i < exitSpawns.size(); i++) {
            exits.put(i, new Exit(i, Position.from(exitSpawns.get(i))));
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

        this.players.clear();
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
