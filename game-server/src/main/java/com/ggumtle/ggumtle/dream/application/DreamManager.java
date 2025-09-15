package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.application.command.AttackWithItemCommand;
import com.ggumtle.ggumtle.dream.application.command.HitMonggingCommand;
import com.ggumtle.ggumtle.dream.application.body.DigUpReceiveBody;
import com.ggumtle.ggumtle.dream.application.body.DigUpBody;
import com.ggumtle.ggumtle.dream.application.body.DoneReviveBody;
import com.ggumtle.ggumtle.dream.application.body.DreamEndBody;
import com.ggumtle.ggumtle.dream.application.body.ExitOpen;
import com.ggumtle.ggumtle.dream.application.body.EscapeBody;
import com.ggumtle.ggumtle.dream.application.body.FeedDoneBody;
import com.ggumtle.ggumtle.dream.application.body.HitMonggingBody;
import com.ggumtle.ggumtle.dream.application.body.InitializeMapBody;
import com.ggumtle.ggumtle.dream.application.body.InitializePlayerBody;
import com.ggumtle.ggumtle.dream.application.body.MongdungSkillBody;
import com.ggumtle.ggumtle.dream.application.body.MonggingStatusBody;
import com.ggumtle.ggumtle.dream.application.body.NewGgumtleBody;
import com.ggumtle.ggumtle.dream.application.body.PutItemBody;
import com.ggumtle.ggumtle.dream.application.body.StartReviveBody;
import com.ggumtle.ggumtle.dream.application.body.StopReviveBody;
import com.ggumtle.ggumtle.dream.application.body.TakeItemBody;
import com.ggumtle.ggumtle.dream.application.body.PlayerMoveBody;
import com.ggumtle.ggumtle.dream.application.body.ShowBoxBody;
import com.ggumtle.ggumtle.dream.application.body.StartFeedBody;
import com.ggumtle.ggumtle.dream.application.body.StopDiggingBody;
import com.ggumtle.ggumtle.dream.application.body.StopFeedingBody;
import com.ggumtle.ggumtle.dream.application.body.UseFieldItemBody;
import com.ggumtle.ggumtle.dream.application.body.UseMonggingItemBody;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Exit;
import com.ggumtle.ggumtle.dream.domain.FakeGgumtle;
import com.ggumtle.ggumtle.dream.domain.FieldItem;
import com.ggumtle.ggumtle.dream.domain.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.Mongdung;
import com.ggumtle.ggumtle.dream.domain.Mongging;
import com.ggumtle.ggumtle.dream.domain.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.dream.util.ItemDistributor;
import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.ExitSpawn;
import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import com.ggumtle.ggumtle.dream.domain.Item;
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
    private final Map<Integer, FieldItem> fieldItems;
    private final Map<Integer, Exit> exits;
    private final AtomicBoolean isExitOpen;

    public DreamManager(Room room, SpawnCache spawnCache) {
        log.info("{}번 게임 생성 시작", room.id);

        this.spawnCache = spawnCache;
        this.room = room;
        this.players = new HashMap<>();
        this.escapedMonggings = ConcurrentHashMap.newKeySet();
        this.ggumtles = new ConcurrentHashMap<>();
        this.ggumtleIdGenerator = new AtomicInteger(0);
        this.boxes = new HashMap<>();
        this.fieldItems = new HashMap<>();
        this.exits = new HashMap<>();
        this.isExitOpen = new AtomicBoolean(false);

        log.info("{}번 게임의 초기화 시작", room.id);

        initializeMap();
        initializePlayers();

        this.workerThreadPool = Executors.newScheduledThreadPool(players.size());
        this.workingThreads = new ConcurrentHashMap<>();

        log.info("{}번 게임의 초기화 종료", room.id);
    }

    public void movePlayer(Session session, int x, int y, int z) {
        Position position = new Position(x, y, z, System.currentTimeMillis());

        Player player = players.get(session.getMemberId());
        player.addPosition(position);

        Body body = new PlayerMoveBody(player.getId(), x, y, z);
        Packet packet = Packet.of(SendPacketType.PLAYER_MOVE_RELAY, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.debug("[{} - {}] {}번 사용자의 이동 핸들링: {{}, {}, {}}", session.getChannel().id(), room.id, session.getMemberId(), x, y, z);
    }

    public void hitMongging(HitMonggingCommand command, Session session, long timestamp) {
        Player requester = players.getOrDefault(session.getMemberId(), null);

        if (requester == null) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_PLAYER, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽둥이의 타격 실패: 요청자 {}번 사용자를 찾을 수 없음", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        if (!(requester instanceof Mongdung mongdung)) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_MONGDUNG, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽둥이의 타격 실패: 요청자 {}번 사용자가 몽둥이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        Player targetPlayer = players.getOrDefault(command.targetId(), null);
        if (targetPlayer == null) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_FOUND_TARGET, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽둥이의 타격 실패: 대상 {}번 사용자를 찾을 수 없음", session.getChannel().id(), room.id, command.targetId());
            return;
        }

        if (!(targetPlayer instanceof Mongging targetMongging)) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_MONGGING, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽둥이의 타격 실패: 대상 {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, command.targetId());
            return;
        }

        boolean isHit = mongdung.detectHit(command.vx(), command.vy(), command.vz(), timestamp, targetMongging);

        if (!isHit) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.FAIL, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.info("[{} - {}] 몽둥이의 타격 성공: {}번 몽깅이가 타격 범위 내에 없음", session.getChannel().id(), room.id, command.targetId());
            return;
        }

        int damage = mongdung.getDamage();
        int leftHp = targetMongging.getHit(damage);

        Body body = new HitMonggingBody(HitMonggingBody.Result.SUCCESS, leftHp);
        Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
        room.sendPacket(List.of(mongdung.getId(), targetMongging.getId()), packet);

        log.info("[{} - {}] 몽둥이의 타격 성공: {}번 몽깅이 타격, 대미지: {}, 남은 HP: {}", session.getChannel().id(), room.id, command.targetId(), damage, leftHp);

        if (leftHp == 0) {
            body = new MonggingStatusBody(targetMongging.getId(), targetMongging.isDead() ? MonggingStatusBody.Result.DEAD : MonggingStatusBody.Result.KNOCKOUT);
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            WorkingThread removedThread = workingThreads.remove(session.getMemberId());
            if (removedThread != null) {
                removedThread.scheduledFuture.cancel(true);
            }

            distributeDroppedItem(targetMongging);

            log.info("[{} - {}] {}번 몽깅이 사망!", session.getChannel().id(), room.id, command.targetId());
        }
    }

    public void startRevive(long targetMonggingId, Session session) {
        Player player = players.getOrDefault(session.getMemberId(), null);
        Player targetPlayer = players.getOrDefault(targetMonggingId, null);
        if (player == null || targetPlayer == null) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_FOUND_PLAYER);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽깅이 부활 시작 실패: 요청자 {}번 사용자 혹은 대상 {}번 사용자를 찾을 수 없음", session.getChannel().id(), room.id, session.getMemberId(), targetMonggingId);
            return;
        }

        if (!(targetPlayer instanceof Mongging targetMongging) || !(player instanceof Mongging)) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_MONGGING);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 부활 시작 실패: 요청자 {}번 사용자 혹은 대상 {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId(), targetMonggingId);
            return;
        }

        if (!targetMongging.isKnockout()) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_KNOCKOUT);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽깅이 부활 시작 실패: 대상 {}번 사용자가 기절 상태가 아님", session.getChannel().id(), room.id, targetMonggingId);
            return;
        }

        // TODO: 몽깅이 인근인지 확인

        ScheduledFuture<?> future = workerThreadPool.schedule(
                () -> {
                    targetMongging.revive();

                    Body body = new DoneReviveBody(targetMongging.getId());
                    Packet packet = Packet.of(SendPacketType.DONE_REVIVE, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    body = new MonggingStatusBody(targetMongging.getId(), MonggingStatusBody.Result.NORMAL);
                    packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                    this.room.broadcast(packet);

                    workingThreads.remove(session.getMemberId());

                    log.info("[{} - {}] 몽깅이 부활 완료: {}번 몽깅이 부활 성공!", session.getChannel().id(), room.id, targetMonggingId);
                }, 3, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.REVIVE, targetMongging.getId()));

        Body body = new StartReviveBody(StartReviveBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
        session.sendPacket(packet);
        log.info("[{} - {}] 몽깅이 부활 시작 성공: 잠시 후 {}번 몽깅이 부활 예정", session.getChannel().id(), room.id, targetMonggingId);
    }

    public void stopRevive(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Body body;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.REVIVE) {
            workingThreads.remove(session.getMemberId());
            body = new StopReviveBody(StopReviveBody.Result.SUCCESS);
            log.info("[{} - {}] 몽깅이 부활 종료 성공", session.getChannel().id(), room.id);
        } else {
            body = new StopReviveBody(StopReviveBody.Result.FAIL);
            log.warn("[{} - {}] 몽깅이 부활 종료 실패: {}번 사용자에게 진행 중인 부활 작업이 없음", session.getChannel().id(), room.id, session.getMemberId());
        }
        Packet packet = Packet.of(SendPacketType.STOP_REVIVE, System.currentTimeMillis(), body);
        session.sendPacket(packet);
    }

    public void doSkill(int skillTypeId, Session session) {
        Mongdung.SkillType skillType = Mongdung.SkillType.valueById(skillTypeId);
        if (skillType == null) {
            Body body = new MongdungSkillBody(skillTypeId, MongdungSkillBody.Result.NOT_FOUND_SKILL);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽둥이 스킬 실패: {}번에 해당하는 스킬 없음", session.getChannel().id(), room.id, skillTypeId);
            return;
        }

        Player player = players.getOrDefault(session.getMemberId(), null);
        if (!(player instanceof Mongdung mongdung)) {
            Body body = new MongdungSkillBody(skillTypeId, MongdungSkillBody.Result.NOT_FOUND_MONGDUNG);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽둥이 스킬 실패: {}번 사용자가 없거나 몽둥이가 아님", session.getChannel().id(), room.id, session.getMemberId());
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

    public void attackWithItem(AttackWithItemCommand command, Session session) {
        Item item = Item.valueOf(command.itemId());
        if (item == null) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_ITEM_ID, command.itemId());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번에 해당하는 아이템 없음", session.getChannel().id(), room.id, command.itemId());
            return;
        }

        if (!item.isAttackItem()) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_ATTACK_ITEM, item.id);
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 아이템은 공격형 아이템이 아님", session.getChannel().id(), room.id, command.itemId());
            return;
        }

        Player player = players.getOrDefault(session.getMemberId(), null);
        if (!(player instanceof Mongging mongging)) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_MONGGING, item.id);
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        Player mongdungPlayer = players.values().stream().filter(p -> p instanceof Mongdung).findFirst().orElse(null);
        if (mongdungPlayer == null) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_ITEM, item.id);
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        Item usedItem = mongging.popItem(item);
        if (usedItem == null) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_ITEM, item.id);
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 몽깅이에게 {} 아이템이 없음", session.getChannel().id(), room.id, session.getMemberId(), usedItem);
            return;
        }

        // TODO: 히트 판정
        Mongdung mongdung = (Mongdung) mongdungPlayer;

        Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.SUCCESS, item.id);
        Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
        session.sendPacket(packet);
        this.room.sendPacket(mongdung.getId(), packet);

        log.error("[{} - {}] 몽깅이 아이템 공격 성공", session.getChannel().id(), room.id);
    }

    public void useFieldItem(int itemId, Session session) {
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (!(player instanceof Mongging mongging)) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.NOT_MONGGING, itemId);
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 필드 아이템 사용 실패: 요청자 {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        FieldItem fieldItem = fieldItems.getOrDefault(itemId, null);
        if (fieldItem == null) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.NOT_FOUND_FIELD_ITEM, itemId);
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 필드 아이템 사용 실패: {}번에 해당하는 필드 아이템이 없음", session.getChannel().id(), room.id, itemId);
            return;
        }

        if (fieldItem.isUsed()) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.ALREADY_USED, fieldItem.id);
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 필드 아이템 사용 실패: {}번 필드 아이템을 이미 사용함", session.getChannel().id(), room.id, itemId);
            return;
        }

        // TODO: 필드템 인근인지 확인

        fieldItem.use();

        Body body = new UseFieldItemBody(UseFieldItemBody.Result.SUCCESS, fieldItem.id);
        Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 필드 아이템 사용 실패: {}번 필드 아이템 사용", session.getChannel().id(), room.id, itemId);
    }

    public void showBox(int boxId, Session session) {
        if (!boxes.containsKey(boxId)) {
            Body body = new ShowBoxBody(false, -1, null);
            Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자 오픈 실패: {}번 상자가 없음", session.getChannel().id(), room.id, boxId);
            return;
        }

        // TODO: 상자 근처인지 확인

        Box box = boxes.get(boxId);
        Item[] items = box.getItems();
        box.addViewer(session);

        Body body = new ShowBoxBody(true, boxId, items);
        Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        log.info("[{} - {}] 상자 오픈 성공: {}번 상자에 {}번 세션 추가", session.getChannel().id(), room.id, boxId, session.getSessionId());
    }

    public void closeBox(int boxId, Session session) {
        Box box = boxes.getOrDefault(boxId, null);

        if (box == null) {
            log.error("[{} - {}] 상자 닫기 실패: {}번 상자가 없음", session.getChannel().id(), room.id, boxId);
            return;
        }

        box.removeViewer(session);

        log.info("[{} - {}] 상자 닫기 성공: {}번 상자에 {}번 세션 삭제", session.getChannel().id(), room.id, boxId, session.getSessionId());
    }

    public void takeItem(int boxId, int index, Session session) {
        // 상자 존재 검사
        Box box = boxes.getOrDefault(boxId, null);
        if (box == null) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 상자가 없음", session.getChannel().id(), room.id, boxId);
            return;
        }

        // 인덱스 검사
        if (index < 0 || index >= Box.BOX_SIZE) {
            Body body = new TakeItemBody(TakeItemBody.Result.INDEX_OUT_OF_RANGE, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: 인덱스 범위 오류 - {}", session.getChannel().id(), room.id, index);
            return;
        }

        // 플레이어 검사
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (player == null) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 상자에서 아이템 꺼내기 실패: 요청자 {}번 사용자가 없음", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        if (!(player instanceof Mongging mongging)) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: 요청자 {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        // TODO: 상자 근처인지 확인

        // 아이템 이동
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));

        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                Item targetItem = box.getItemAt(index);
                if (targetItem == null) {
                    Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_BOX, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    log.warn("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 상자의 {}번째 칸에 아이템이 없음", session.getChannel().id(), room.id, boxId, index);
                    return;
                }

                if (!mongging.canAddItem(targetItem)) {
                    Body body = new TakeItemBody(TakeItemBody.Result.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    log.info("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 몽깅이의 인벤토리에 빈 공간이 없음", session.getChannel().id(), room.id, session.getMemberId());
                    return;
                }

                Item[] popResult = box.popItem(index);
                boolean success = mongging.addItem(popResult[Box.BOX_SIZE]);

                if (!success) {
                    log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 몽깅이가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패. 인벤토리: {}",
                            session.getChannel().id(),
                            session.getMemberId(),
                            targetItem,
                            popResult[Box.BOX_SIZE],
                            Arrays.deepToString(mongging.getItems()));

                    Body body = new TakeItemBody(TakeItemBody.Result.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);
                    return;
                }

                Body body = new TakeItemBody(TakeItemBody.Result.SUCCESS, popResult);
                Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                session.sendPacket(packet);

                log.info("[{} - {}] 상자에서 아이템 꺼내기 성공: {}번 몽깅이가 {} 아이템 획득", session.getChannel().id(), room.id, session.getMemberId(), targetItem);
            }
        }
    }

    public void putItem(int itemId, int boxId, Session session) {
        Item targetItem = Item.valueOf(itemId);
        if (targetItem == null) {
            Body body = new PutItemBody(PutItemBody.Result.ILLEGAL_ITEM_ID, null);
            Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 아이디에 대응하는 아이템 없음", session.getChannel().id(), room.id, itemId);
            return;
        }

        // 상자 존재 검사
        Box box = boxes.getOrDefault(boxId, null);
        if (box == null) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 아이디에 대응하는 상자 없음", session.getChannel().id(), room.id, boxId);
            return;
        }

        // 플레이어 검사
        Player player = players.getOrDefault(session.getMemberId(), null);
        if (player == null) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 상자에 아이템 넣기 실패: 요청자 {}번 사용자가 없음", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        if (!(player instanceof Mongging mongging)) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 상자에 아이템 넣기 실패: 요청자 {}번 사용자가 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        // TODO: 상자 근처인지 확인

        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));
        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                int count = mongging.countItem(targetItem);
                if (count == 0) {
                    Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    log.warn("[{} - {}] 상자에 아이템 넣기 실패: {}번 몽깅이에게 {}번 아이템이 없음", session.getChannel().id(), room.id, session.getMemberId(), itemId);
                    return;
                }

                if (box.isFull()) {
                    Body body = new PutItemBody(PutItemBody.Result.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    log.info("[{} - {}] 상자에 아이템 넣기 실패: {}번 상자에 빈 공간이 없음", session.getChannel().id(), room.id, boxId);
                    return;
                }

                Item poppedItem = mongging.popItem(targetItem);
                boolean success = box.addItem(poppedItem);

                if (!success) {
                    Body body = new PutItemBody(PutItemBody.Result.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    session.sendPacket(packet);

                    log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 상자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패. 상자: {}",
                            session.getChannel().id(),
                            boxId,
                            targetItem,
                            poppedItem,
                            Arrays.deepToString(mongging.getItems()));
                    return;
                }

                Body body = new PutItemBody(PutItemBody.Result.SUCCESS, box.getItems());
                Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                session.sendPacket(packet);

                log.info("[{} - {}] 상자에 아이템 넣기 성공: {}번 상자에 {} 아이템 추가", session.getChannel().id(), room.id, boxId, itemId);
                return;
            }
        }
    }

    public void digUpGgumtle(int ggumtleId, Session session) {
        if (!ggumtles.containsKey(ggumtleId)) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.NOT_FOUND);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 꿈틀이 파기 시작 실패: {}번 아이디에 대응하는 꿈틀이가 없음", session.getChannel().id(), room.id, ggumtleId);
            return;
        }

        Ggumtle ggumtle = ggumtles.get(ggumtleId);
        if (ggumtle.isDugUp()) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.ALREADY_DIG_UP);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 꿈틀이 파기 시작 실패: {}번 꿈틀이가 이미 파짐", session.getChannel().id(), room.id, ggumtleId);
            return;
        }

        // TODO: 꿈틀이 근처인지 확인

        ScheduledFuture<?> future = workerThreadPool.schedule(() -> {
            Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
            while (iterator.hasNext()) {
                Map.Entry<Long, WorkingThread> entry = iterator.next();
                if (entry.getValue().threadType == WorkingThread.ThreadType.DIG_UP && entry.getValue().ggumtleId == ggumtle.id) {
                    entry.getValue().scheduledFuture.cancel(true);
                    iterator.remove();
                }
            }

            int digUpResult = ggumtle.tryDigUp();

            // 3초를 기다리는 동안 누군가 파냈으면 무시
            if (digUpResult == 0) {
                log.warn("[{} - {}] 꿈틀이 파기 무시: 해당 작업이 기다리는 동안 {}번 꿈틀이가 파져서 무시", session.getChannel().id(), room.id, ggumtleId);
                return;
            }

            Body body = new DigUpBody(ggumtle.id, digUpResult == 1);
            Packet packet = Packet.of(SendPacketType.DIG_UP_DONE, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 꿈틀이 파기 완료: {}번 꿈틀이 파기 완료", session.getChannel().id(), room.id, ggumtleId);
            // TODO: 스턴 상태 브로드캐스팅
        }, 3, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.DIG_UP, ggumtle.id));

        Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.START_DIGGING);
        Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        log.info("[{} - {}] 꿈틀이 파기 시작 완료: 잠시 후 {}번 꿈틀이 파기 완료 예정", session.getChannel().id(), room.id, ggumtleId);
    }

    public void stopDigging(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Body body;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.DIG_UP) {
            body = new StopDiggingBody(StopDiggingBody.Result.STOP);
            workingThreads.remove(session.getMemberId());

            log.info("[{} - {}] 꿈틀이 파기 중단 완료: {}번 사용자의 작업 중단", session.getChannel().id(), room.id, session.getMemberId());
        } else {
            body = new StopDiggingBody(StopDiggingBody.Result.NOT_FOUND_DIGGING);

            log.warn("[{} - {}] 꿈틀이 파기 중단 실패: {}번 사용자가 꿈틀이 파내기 작업 없음", session.getChannel().id(), room.id, session.getMemberId());
        }
        Packet packet = Packet.of(SendPacketType.STOP_DIGGING, System.currentTimeMillis(), body);
        session.sendPacket(packet);
    }

    public void startFeed(int ggumtleId, Session session) {
        if (!ggumtles.containsKey(ggumtleId)) {
            Body body = new StartFeedBody(StartFeedBody.Result.NOT_FOUND);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 아이디에 대응하는 꿈틀이 없음", session.getChannel().id(), room.id, ggumtleId);
            return;
        }

        Ggumtle ggumtle = ggumtles.get(ggumtleId);
        if (!ggumtle.isDugUp()) {
            Body body = new StartFeedBody(StartFeedBody.Result.YET_DIG_UP);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 꿈틀이가 아직 파지지 않음", session.getChannel().id(), room.id, ggumtleId);
            return;
        }

        if (ggumtle.isDone()) {
            Body body = new StartFeedBody(StartFeedBody.Result.ALREADY_DONE);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 꿈틀이가 이미 정화됨", session.getChannel().id(), room.id, ggumtleId);
            return;
        }

        Mongging mongging = (Mongging) players.get(session.getMemberId());
        int count = mongging.countItem(Item.GGUMTLE_FEED);
        if (count == 0) {
            Body body = new StartFeedBody(StartFeedBody.Result.LACK_OF_FEED_ITEM);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 몽깅이에게 꿈젤리가 없음", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        // TODO: 꿈틀이 근처인지 확인

        Runnable task = () -> {
            final int left = ggumtle.feed();
            mongging.popItem(Item.GGUMTLE_FEED);

            // 성불시키면 종료
            if (left <= 0) {
                Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
                while (iterator.hasNext()) {
                    Map.Entry<Long, WorkingThread> entry = iterator.next();
                    if (entry.getValue().ggumtleId == ggumtle.id) {
                        entry.getValue().scheduledFuture.cancel(true);
                        iterator.remove();
                    }
                }

                Body body = new StopFeedingBody(StopFeedingBody.Result.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
                session.sendPacket(packet);

                body = new FeedDoneBody(ggumtle.id);
                packet = Packet.of(SendPacketType.FEED_DONE, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                tryOpenExit();

                log.info("[{} - {}] 꿈틀이 먹이기 완료: 꿈틀이가 성불해 종료", session.getChannel().id(), room.id);
                return;
            }

            // 남은 아이템이 없으면 종료
            int leftFeedItem = mongging.countItem(Item.GGUMTLE_FEED);
            if (leftFeedItem == 0) {
                Body body = new StopFeedingBody(StopFeedingBody.Result.STOP, mongging.countItem(Item.GGUMTLE_FEED));
                Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
                session.sendPacket(packet);

                WorkingThread removedThread = workingThreads.remove(session.getMemberId());
                removedThread.scheduledFuture.cancel(true);

                log.info("[{} - {}] 꿈틀이 먹이기 완료: 아이템을 모두 소진해 종료", session.getChannel().id(), room.id);
                return;
            }
        };
        ScheduledFuture<?> future = workerThreadPool.scheduleAtFixedRate(task, 1, 1, TimeUnit.SECONDS);
        workingThreads.put(session.getMemberId(), new WorkingThread(session.getMemberId(), future, WorkingThread.ThreadType.FEED, ggumtle.id));

        Body body = new StartFeedBody(StartFeedBody.Result.START_FEEDING);
        Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        log.info("[{} - {}] 꿈틀이 먹이기 시작 성공: {}번 사용자가 {}번 꿈틀이에게 꿈젤리 먹이기 시작함", session.getChannel().id(), room.id, session.getMemberId(), ggumtleId);
    }

    public void stopFeeding(Session session) {
        WorkingThread targetThread = workingThreads.getOrDefault(session.getMemberId(), null);

        Mongging mongging = (Mongging) players.get(session.getMemberId());
        int leftItemCount = mongging.countItem(Item.GGUMTLE_FEED);

        Body body;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.FEED) {
            body = new StopFeedingBody(StopFeedingBody.Result.STOP, leftItemCount);
            workingThreads.remove(session.getMemberId());

            log.info("[{} - {}] 꿈틀이 먹이기 종료 성공: {}번 사용자의 꿈젤리 먹이기 작업 종료", session.getChannel().id(), room.id, session.getMemberId());
        } else {
            body = new StopFeedingBody(StopFeedingBody.Result.NOT_FOUND, leftItemCount);

            log.error("[{} - {}] 꿈틀이 먹이기 종료 실패: {}번 사용자에게 꿈젤리 먹이기 작업 없음", session.getChannel().id(), room.id, session.getMemberId());
        }
        Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
        session.sendPacket(packet);
    }

    public void tryOpenExit() {
        for (Ggumtle ggumtle : ggumtles.values()) {
            if (!ggumtle.isDone()) {
                log.info("{}번 게임에 아직 정화되지 않은 꿈틀이가 있어 탈출구가 열리지 않음", this.room.id);
                return;
            }
        }

        boolean isExpected = isExitOpen.compareAndSet(false, true);
        if (!isExpected) {
            log.warn("{}번 게임의 탈출구 오픈이 다시 이루어졌습니다", this.room.id);
            return;
        }

        Body body = new ExitOpen(this.exits.values().stream().toList());
        Packet packet = Packet.of(SendPacketType.OPEN_EXIT, System.currentTimeMillis(), body);
        this.room.broadcast(packet);
        log.info("{}번 게임에 탈출구가 열림!", this.room.id);
    }

    public void escape(int exitId, Session session) {
        Exit exit = exits.getOrDefault(exitId, null);

        if (exit == null) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_FOUND_EXIT);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: {}번 아이디에 해당하는 문이 없음", session.getChannel().id(), room.id, exitId);
            return;
        }

        Player player = players.get(session.getMemberId());
        if (!(player instanceof Mongging mongging)) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_MONGGING);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: 요청 {}번 사용자는 몽깅이가 아님", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        // TODO: 위치 검사

        if (!mongging.isNotDead()) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_ALIVE);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: {}번 몽깅이는 죽어서 탈출이 불가능", session.getChannel().id(), room.id, session.getMemberId());
            return;
        }

        mongging.escape();
        escapedMonggings.add(mongging.getId());

        Body body = new EscapeBody(EscapeBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.ESCAPE);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽깅이 탈출 성공: {}번 몽깅이 탈출 성공", session.getChannel().id(), room.id, session.getMemberId());

        if (escapedMonggings.size() >= WINNING_MONGGING_COUNT) {
            body = new DreamEndBody(DreamEndBody.Result.MONGGING_WIN, escapedMonggings, players.values());
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 몽깅이 승리: 몽깅이가 탈출 조건보다 많이 탈출하여 승리", session.getChannel().id(), room.id);
        }
    }

    private void makeScare(Mongdung mongdung, Session session) {
        boolean success = mongdung.scare();

        if (!success) {
            Body body = new MongdungSkillBody(Mongdung.SkillType.SCARE, MongdungSkillBody.Result.YET_COOL_TIME);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽둥이 공포 스킬 실패: 쿨타임 부족", session.getChannel().id(), room.id);
            return;
        }

        Body body = new MongdungSkillBody(Mongdung.SkillType.SCARE, MongdungSkillBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽둥이 공포 스킬 성공", session.getChannel().id(), room.id);
    }

    private void buryFakeGgumtle(Mongdung mongdung, Session session) {
        Position position = mongdung.getPositionAt(System.currentTimeMillis());

        boolean success = mongdung.tryBuryFakeGgumtle();

        if (!success) {
            Body body = new MongdungSkillBody(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillBody.Result.LACK_USE_COUNT);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            session.sendPacket(packet);

            log.warn("[{} - {}] 몽둥이 가짜 꿈틀이 스킬 실패: 사용 횟수 소진", session.getChannel().id(), room.id);
            return;
        }

        Body body = new MongdungSkillBody(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
        session.sendPacket(packet);

        int id = ggumtleIdGenerator.addAndGet(1);
        FakeGgumtle fakeGgumtle = new FakeGgumtle(id, position);

        this.ggumtles.put(id, fakeGgumtle);

        body = new NewGgumtleBody(fakeGgumtle.id, fakeGgumtle.position);
        packet = Packet.of(SendPacketType.NEW_GGUMTLE, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽둥이 가짜 꿈틀이 스킬 성공", session.getChannel().id(), room.id);
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
            ItemDistributor.distribute(item, boxes, item.initialCount);
        }

        // 필드 아이템 초기화
        List<FieldItemSpawn> fieldItemSpawns = spawnCache.getFieldItemSpawns();
        for (FieldItemSpawn spawn : fieldItemSpawns) {
            FieldItem fieldItem = new FieldItem(spawn);
            fieldItems.put(fieldItem.id, fieldItem);
        }

        // 출구 초기화
        List<ExitSpawn> exitSpawns = spawnCache.getRandomExitSpawns();
        for (int i = 0; i < exitSpawns.size(); i++) {
            exits.put(i, new Exit(i, Position.from(exitSpawns.get(i))));
        }

        log.info("{}번 게임의 맵 초기화 종료", room.id);

        List<FieldItem> healPacks = new ArrayList<>();
        List<FieldItem> speedPacks = new ArrayList<>();
        fieldItems.values().forEach(fieldItem -> {
            if (fieldItem.type == FieldItem.Type.HEAL) {
                healPacks.add(fieldItem);
                return;
            }
            if (fieldItem.type == FieldItem.Type.SPEED) {
                speedPacks.add(fieldItem);
                return;
            }
        });

        Body body = new InitializeMapBody(
                new ArrayList<>(this.boxes.values()),
                new ArrayList<>(this.ggumtles.values()),
                healPacks,
                speedPacks);
        Packet packet = Packet.of(SendPacketType.INITIALIZE_MAP, System.currentTimeMillis(), body);
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

        log.info("{}번 게임의 플레이어 초기화 종료", room.id);
        log.debug("{}번 게임의 플레이어: {}", room.id, this.players.values());

        List<Player> players = this.players.values().stream().toList();
        for (long playerId : playerIds) {
            Body body = new InitializePlayerBody(players, playerId);
            Packet packet = Packet.of(SendPacketType.INITIALIZE_PLAYER, System.currentTimeMillis(), body);
            boolean success = room.sendPacket(playerId, packet);
            if (!success) {
                log.error("{}번 사용자에게 {}번 게임의 플레이어 초기 정보를 전송하지 못했습니다", playerId, this.room.id);
            }
        }
    }

    private int pickMongdungIndex(int size) {
        Random random = new Random();

        return random.nextInt(size);
    }
}
