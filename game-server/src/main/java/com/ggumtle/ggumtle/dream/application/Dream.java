package com.ggumtle.ggumtle.dream.application;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.common.event.DreamEndEvent;
import com.ggumtle.ggumtle.dream.application.body.CloseBoxBody;
import com.ggumtle.ggumtle.dream.application.body.DreamEndBody;
import com.ggumtle.ggumtle.dream.application.body.GgumtleFedJellyCountBody;
import com.ggumtle.ggumtle.dream.application.body.LeftJellyCountBody;
import com.ggumtle.ggumtle.dream.application.body.UseDefibrillatorBody;
import com.ggumtle.ggumtle.dream.application.result.DreamState;
import com.ggumtle.ggumtle.dream.application.tickevent.*;
import com.ggumtle.ggumtle.dream.application.body.DigUpReceiveBody;
import com.ggumtle.ggumtle.dream.application.body.GgumtleStatusBody;
import com.ggumtle.ggumtle.dream.application.body.DoneReviveBody;
import com.ggumtle.ggumtle.dream.application.body.ExitOpen;
import com.ggumtle.ggumtle.dream.application.body.EscapeBody;
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
import com.ggumtle.ggumtle.dream.application.result.GetHitResult;
import com.ggumtle.ggumtle.dream.domain.item.Attackable;
import com.ggumtle.ggumtle.dream.domain.item.Box;
import com.ggumtle.ggumtle.dream.domain.exit.Exit;
import com.ggumtle.ggumtle.dream.domain.ggumtle.FakeGgumtle;
import com.ggumtle.ggumtle.dream.domain.item.Boxable;
import com.ggumtle.ggumtle.dream.domain.item.FieldItem;
import com.ggumtle.ggumtle.dream.domain.ggumtle.Ggumtle;
import com.ggumtle.ggumtle.dream.domain.item.Flash;
import com.ggumtle.ggumtle.dream.domain.item.Taser;
import com.ggumtle.ggumtle.dream.domain.player.Mongdung;
import com.ggumtle.ggumtle.dream.domain.player.Mongging;
import com.ggumtle.ggumtle.dream.domain.player.Player;
import com.ggumtle.ggumtle.dream.persistence.SpawnCache;
import com.ggumtle.ggumtle.dream.util.ItemDistributor;
import com.ggumtle.ggumtle.dream.vo.BoxSpawn;
import com.ggumtle.ggumtle.dream.vo.ExitSpawn;
import com.ggumtle.ggumtle.dream.vo.FieldItemSpawn;
import com.ggumtle.ggumtle.dream.vo.GgumtleSpawn;
import com.ggumtle.ggumtle.dream.domain.item.ItemDictionary;
import com.ggumtle.ggumtle.dream.vo.PlayerSpawn;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.room.domain.Room;
import com.ggumtle.ggumtle.server.application.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import io.netty.channel.Channel;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Comparator;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Random;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;

@Slf4j
public class Dream {

    private static final int BOX_SPAWN_SIZE = 20;
    private static final int GGUMTLE_SPAWN_SIZE = 3;
    private static final int WINNING_MONGGING_COUNT = 2;
    private static final long PLAY_TIME = 15 * 60 * 1000L;

    private final ScheduledExecutorService workerThreadPool;
    private final ConcurrentHashMap<Long, WorkingThread> workingThreads;
    private final ScheduledExecutorService timerThread;
    private ScheduledFuture<?> timerFuture;
    private final ApplicationEventPublisher applicationEventPublisher;

    // 정적 데이터 캐싱
    private final SpawnCache spawnCache;

    // 인게임 캐시
    private final Room room;
    private final Map<Long, Player> players;
    private final ConcurrentHashMap<Integer, Ggumtle> ggumtles;
    private int ggumtleIdGenerator;
    private final Map<Integer, Box> boxes;
    private final Map<Integer, FieldItem> fieldItems;
    private final Map<Integer, Exit> exits;
    private final AtomicBoolean isExitOpen;

    public Dream(Room room, SpawnCache spawnCache, ApplicationEventPublisher applicationEventPublisher) {
        this.applicationEventPublisher = applicationEventPublisher;

        log.info("{}번 드림 생성 시작", room.id);

        this.spawnCache = spawnCache;
        this.room = room;
        this.players = new HashMap<>();
        this.ggumtles = new ConcurrentHashMap<>();
        this.ggumtleIdGenerator = 0;
        this.boxes = new HashMap<>();
        this.fieldItems = new HashMap<>();
        this.exits = new HashMap<>();
        this.isExitOpen = new AtomicBoolean(false);

        log.info("{}번 드림의 초기화 시작", room.id);

        initializeMap();
        initializePlayers();

        this.workerThreadPool = Executors.newScheduledThreadPool(players.size());
        this.workingThreads = new ConcurrentHashMap<>();
        this.timerThread = Executors.newScheduledThreadPool(1);

        log.info("{}번 드림의 초기화 종료", room.id);
    }

    public void setTimer(long startTimestamp) {
        long delay = startTimestamp + PLAY_TIME - System.currentTimeMillis();

        if (this.room.id < 0) {
            timerFuture = timerThread.schedule(() -> log.info("테스트방이라서 게임이 종료되지 않음"), delay, TimeUnit.MILLISECONDS);
        } else {
            timerFuture = timerThread.schedule(() -> endDream(false), delay, TimeUnit.MILLISECONDS);
        }

        log.info("{}번 드림의 타이머 설정 완료: 시작 시간 = {}, 딜레이 = {}", room.id, startTimestamp, delay);
    }

    public DreamState getDreamState() {
        return new DreamState(
                this.boxes.values().stream().toList(),
                this.ggumtles.values().stream().toList(),
                this.fieldItems.values().stream().filter((item) -> item.type == FieldItem.Type.HEAL).toList(),
                this.fieldItems.values().stream().filter((item) -> item.type == FieldItem.Type.SPEED).toList(),
                this.players.values().stream().toList()
        );
    }

    public void on(PlayerMoveTickEvent event) {
        long now = System.currentTimeMillis();

        Player player = players.get(ChannelManager.getMemberId(event.channel()));

        Position lastPosition = player.getLastPosition();
        Position currentPosition = new Position(event.command().x(), event.command().y(), event.command().z(), now);

        int distanceSquare = lastPosition.getDistanceSquareWith(currentPosition);
        double maxDistance = player.moveSpeed * (now - lastPosition.timestamp);

        if (distanceSquare > maxDistance * maxDistance) {
            Body body = PlayerMoveBody.rollbackOf(player.getId(), lastPosition);
            Packet packet = Packet.of(SendPacketType.PLAYER_MOVE_RELAY, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.warn("[{} - {}] {}번 사용자의 이동 핸들링: 비정상적인 이동 감지: {{}, {}, {}}", event.channel().id(), room.id,
                    player.getId(), event.command().x(), event.command().y(), event.command().z());
            return;
        }

        player.addPosition(currentPosition);

        Body body = new PlayerMoveBody(player.getId(), event.command().x(), event.command().y(), event.command().z(),
                event.command().vx(), event.command().vy(), event.command().vz());
        Packet packet = Packet.of(SendPacketType.PLAYER_MOVE_RELAY, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.debug("[{} - {}] {}번 사용자의 이동 핸들링: {{}, {}, {}}",
                event.channel().id(), room.id, player.getId(), event.command().x(), event.command().y(), event.command().z());
    }

    public void on(HitMonggingTickEvent event) {
        Player requester = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);

        if (requester == null) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_PLAYER, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 몽둥이의 타격 실패: 요청자 {}번 사용자를 찾을 수 없음", event.channel().id(), room.id,
                    ChannelManager.getMemberId(event.channel()));
            return;
        }

        if (!(requester instanceof Mongdung mongdung)) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_MONGDUNG, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽둥이의 타격 실패: 요청자 {}번 사용자가 몽둥이가 아님", event.channel().id(), room.id,
                    ChannelManager.getMemberId(event.channel()));
            return;
        }

        if (event.command().targetId() < 0) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.FAIL, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.warn("[{} - {}] 몽둥이의 타격 실패: 클라이언트에서 실패로 요청", event.channel().id(), room.id);
            return;
        }

        Player targetPlayer = players.getOrDefault(event.command().targetId(), null);
        if (targetPlayer == null) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_FOUND_TARGET, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 몽둥이의 타격 실패: 대상 {}번 사용자를 찾을 수 없음", event.channel().id(), room.id,
                    event.command().targetId());
            return;
        }

        if (!(targetPlayer instanceof Mongging targetMongging)) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_MONGGING, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽둥이의 타격 실패: 대상 {}번 사용자가 몽깅이가 아님", event.channel().id(), room.id,
                    event.command().targetId());
            return;
        }

        // 몽둥이 타격 범위 확인
        boolean isHit = mongdung.detectHit(event.command().vx(), event.command().vy(), event.command().vz(), System.currentTimeMillis(),
                targetMongging);
        if (!isHit) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.FAIL, -1, -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 몽둥이의 타격 성공: {}번 몽깅이가 타격 범위 내에 없음", event.channel().id(), room.id,
                    event.command().targetId());
            return;
        }

        GetHitResult result = targetMongging.getHit(mongdung.damage);

        if (result.result() == GetHitResult.Result.NOT_ALIVE) {
            Body body = new HitMonggingBody(HitMonggingBody.Result.NOT_ALIVE, targetMongging.getId(), -1);
            Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
            this.room.broadcast(packet);
            log.info("[{} - {}] 몽둥이의 타격 실패: {}번 몽깅이가 살아 있지 않음", event.channel().id(), room.id, event.command().targetId());
            return;
        }

        Body body = new HitMonggingBody(HitMonggingBody.Result.SUCCESS, targetMongging.getId(), result.leftHp());
        Packet packet = Packet.of(SendPacketType.HIT, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽둥이의 타격 성공: {}번 몽깅이 타격, 대미지: {}, 남은 HP: {}", event.channel().id(), room.id,
                event.command().targetId(), mongdung.damage, result.leftHp());

        // 몽깅이 기절
        if (result.result() == GetHitResult.Result.KNOCK_OUT) {
            WorkingThread removedThread = workingThreads.remove(requester.getId());
            if (removedThread != null) {
                removedThread.scheduledFuture.cancel(true);
            }

            body = new MonggingStatusBody(targetMongging.getId(), MonggingStatusBody.Result.KNOCKOUT);
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            distributeDroppedItem(targetMongging);

            log.info("[{} - {}] {}번 몽깅이 기절!", event.channel().id(), room.id, event.command().targetId());
        }

        // 몽깅이 사망
        if (result.result() == GetHitResult.Result.DEAD) {
            WorkingThread removedThread = workingThreads.remove(requester.getId());
            if (removedThread != null) {
                removedThread.scheduledFuture.cancel(true);
            }

            body = new MonggingStatusBody(targetMongging.getId(), MonggingStatusBody.Result.DEAD);
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            distributeDroppedItem(targetMongging);

            log.info("[{} - {}] {}번 몽깅이 사망!", event.channel().id(), room.id, event.command().targetId());

            long deadMonggingCount = players.values().stream().filter(p -> p instanceof Mongging m && m.isDead())
                    .count();
            if (deadMonggingCount >= players.size() - 1) {
                endDream(false);

                log.info("[{} - {}] 드림 종료: 모든 몽깅이가 사망함", event.channel().id(), room.id);

            }
        }
    }

    public void on(StartReviveTickEvent event) {
        if (!(players.getOrDefault(ChannelManager.getMemberId(event.channel()), null) instanceof Mongging requesterMongging) ||
                !(players.getOrDefault(event.command().targetMonggingId(), null) instanceof Mongging targetMongging)) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_FOUND_MONGGING);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 부활 시작 실패: 요청자 {}번 사용자 혹은 대상 {}번 사용자를 찾을 수 없거나 몽깅이가 아님",
                    event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()), event.command().targetMonggingId());
            return;
        }

        if (!targetMongging.isKnockout()) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_KNOCKOUT);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 몽깅이 부활 시작 실패: 대상 {}번 사용자가 기절 상태가 아님", event.channel().id(), room.id, targetMongging.getId());
            return;
        }

        long now = System.currentTimeMillis();
        Position requesterPosition = requesterMongging.getPositionAt(now);
        Position targetPosition = targetMongging.getPositionAt(now);
        if (requesterPosition.getDistanceSquareWith(targetPosition) > Mongging.REVIVE_DISTANCE_SQUARE) {
            Body body = new StartReviveBody(StartReviveBody.Result.NOT_NEAR);
            Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 몽깅이 부활 시작 실패: 대상 {}번 몽깅이와 요청한 {}번 몽깅이가 근처에 없음", event.channel().id(), room.id, targetMongging.getId(), requesterMongging.getId());
            return;
        }

        ScheduledFuture<?> future = workerThreadPool.schedule(
                () -> {
                    targetMongging.revive();

                    Body body = new DoneReviveBody(targetMongging.getId());
                    Packet packet = Packet.of(SendPacketType.DONE_REVIVE, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    body = new MonggingStatusBody(targetMongging.getId(), MonggingStatusBody.Result.NORMAL);
                    packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                    this.room.broadcast(packet);

                    workingThreads.remove(requesterMongging.getId());

                    log.info("[{} - {}] 몽깅이 부활 완료: {}번 몽깅이 부활 성공!", event.channel().id(), room.id, targetMongging.getId());
                }, 3500, TimeUnit.MILLISECONDS);
        workingThreads.put(
                requesterMongging.getId(),
                new WorkingThread(requesterMongging.getId(), future, WorkingThread.ThreadType.REVIVE, targetMongging.getId())
        );

        Body body = new StartReviveBody(StartReviveBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.START_REVIVE, System.currentTimeMillis(), body);
        event.channel().write(packet);
        log.info("[{} - {}] 몽깅이 부활 시작 성공: 잠시 후 {}번 몽깅이 부활 예정", event.channel().id(), room.id, targetMongging.getId());
    }

    public void on(StopReviveTickEvent event) {
        Long memberId = ChannelManager.getMemberId(event.channel());
        WorkingThread targetThread = workingThreads.getOrDefault(memberId, null);

        Body body;
        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.REVIVE) {
            targetThread.scheduledFuture.cancel(true);
            workingThreads.remove(memberId);

            body = new StopReviveBody(StopReviveBody.Result.SUCCESS);

            log.info("[{} - {}] 몽깅이 부활 종료 성공", event.channel().id(), room.id);
        } else {
            body = new StopReviveBody(StopReviveBody.Result.FAIL);

            log.warn("[{} - {}] 몽깅이 부활 종료 실패: {}번 사용자에게 진행 중인 부활 작업이 없음", event.channel().id(), room.id, memberId);
        }
        Packet packet = Packet.of(SendPacketType.STOP_REVIVE, System.currentTimeMillis(), body);
        event.channel().write(packet);
    }

    public void on(MongdungSkillTickEvent event) {
        Mongdung.SkillType skillType = Mongdung.SkillType.valueById(event.command().skillTypeId());
        if (skillType == null) {
            Body body = new MongdungSkillBody(event.command().skillTypeId(), MongdungSkillBody.Result.NOT_FOUND_SKILL);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽둥이 스킬 실패: {}번에 해당하는 스킬 없음", event.channel().id(), room.id, event.command().skillTypeId());
            return;
        }

        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (!(player instanceof Mongdung mongdung)) {
            Body body = new MongdungSkillBody(skillType.getId(), MongdungSkillBody.Result.NOT_FOUND_MONGDUNG);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽둥이 스킬 실패: {}번 사용자가 없거나 몽둥이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        if (skillType == Mongdung.SkillType.SCARE) {
            makeScare(mongdung, event.channel());
            return;
        }

        if (skillType == Mongdung.SkillType.FAKE_GGUMTLE) {
            buryFakeGgumtle(mongdung, event.command().x(), event.command().y(), event.command().z(), event.channel());
            return;
        }
    }

    public void on(AttackWithItemTickEvent event) {
        Boxable item = ItemDictionary.valueOf(event.command().itemId());
        if (item == null) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_ITEM_ID, event.command().itemId(),
                    event.command().effectX(), event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번에 해당하는 아이템 없음", event.channel().id(), room.id,
                    event.command().itemId());
            return;
        }

        if (!(item instanceof Attackable<?> attackable)) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_ATTACK_ITEM, item.id, event.command().effectX(),
                    event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 아이템은 공격형 아이템이 아님", event.channel().id(), room.id,
                    event.command().itemId());
            return;
        }

        if (!(players.getOrDefault(ChannelManager.getMemberId(event.channel()), null) instanceof Mongging mongging)) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_MONGGING, item.id,
                    event.command().effectX(), event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: 요청한 {}번 사용자가 몽깅이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        Boxable usedItem = mongging.popItem(item);
        if (usedItem == null) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_ITEM, item.id, event.command().effectX(),
                    event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: {}번 몽깅이에게 {} 아이템이 없음", event.channel().id(), room.id, mongging.getId(), item);
            return;
        }

        Player mongdungPlayer = players.values().stream().filter(p -> p instanceof Mongdung).findFirst().orElse(null);
        if (mongdungPlayer == null) {
            // Body body = new
            // UseMonggingItemBody(UseMonggingItemBody.Result.NOT_FOUND_MONGDUNG, item.id,
            // event.command().effectX(), event.command().effectY(), event.command().effectZ());
            // Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM,
            // System.currentTimeMillis(), body);
            // session.sendPacket(packet);
            // TEST
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.MISS, item.id, event.command().effectX(), event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.error("[{} - {}] 몽깅이 아이템 공격 실패: 드림에 몽둥이가 없음", event.channel().id(), room.id);
            return;
        }

        long now = System.currentTimeMillis();

        Position sourcePosition = mongging.getPositionAt(now);
        Position targetPosition = mongdungPlayer.getPositionAt(now);
        boolean isHit = switch (attackable) {
            case Flash flash -> {
                Flash.HitContext context = new Flash.HitContext(sourcePosition, targetPosition, Mongdung.SIZE, event.command().effectX(), event.command().effectY(), event.command().effectZ());
                yield flash.detectHit(context);
            }

            case Taser taser -> {
                Taser.HitContext context = new Taser.HitContext(sourcePosition, targetPosition, Mongdung.SIZE, event.command().effectX(), event.command().effectY(), event.command().effectZ());
                yield taser.detectHit(context);
            }

            default -> {
                log.error("[{} - {}] 몽깅이 아이템 공격 실패: 지원하지 않는 아이템 사용 - {}", event.channel().id(), room.id,
                        attackable);
                yield false;
            }
        };

        if (!isHit) {
            Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.MISS, item.id, event.command().effectX(), event.command().effectY(), event.command().effectZ());
            Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 몽깅이 아이템 공격 실패: 몽둥이가 {}번 아이템의 피격 범위에 없음", event.channel().id(), room.id, item.id);
            return;
        }

        Body body = new UseMonggingItemBody(UseMonggingItemBody.Result.SUCCESS, item.id, event.command().effectX(), event.command().effectY(), event.command().effectZ());
        Packet packet = Packet.of(SendPacketType.ATTACK_WITH_ITEM, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.error("[{} - {}] 몽깅이 아이템 공격 성공", event.channel().id(), room.id);
    }

    /**
     * 필드 아이템 사용
     */
    public void on(UseFieldItemTickEvent event) {
        // 몽깅이 존재 확인
        if (!(players.getOrDefault(ChannelManager.getMemberId(event.channel()), null) instanceof Mongging mongging)) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.NOT_MONGGING, event.command().itemId());
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 필드 아이템 사용 실패: 요청자 {}번 사용자가 없거나 몽깅이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        // 필드 아이템 존재 확인
        FieldItem fieldItem = fieldItems.getOrDefault(event.command().itemId(), null);
        if (fieldItem == null) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.NOT_FOUND_FIELD_ITEM, event.command().itemId());
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 필드 아이템 사용 실패: {}번에 해당하는 필드 아이템이 없음", event.channel().id(), room.id, event.command().itemId());
            return;
        }

        // 필드템 인근인지 확인
        if (!fieldItem.detectPosition(mongging.getPositionAt(System.currentTimeMillis()))) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.NOT_NEAR, fieldItem.id);
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 필드 아이템 사용 실패: {}번 필드 아이템이 몽깅이 근처에 없음", event.channel().id(), room.id, fieldItem.id);
            return;
        }

        boolean success = fieldItem.use();

        // 필드 아이템 사용 여부 확인
        if (!success) {
            Body body = new UseFieldItemBody(UseFieldItemBody.Result.ALREADY_USED, fieldItem.id);
            Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 필드 아이템 사용 실패: {}번 필드 아이템을 이미 사용함", event.channel().id(), room.id, fieldItem.id);
            return;
        }

        if (fieldItem.type == FieldItem.Type.HEAL) {
            mongging.heal(50);
        }

        // 필드 아이템 사용은 모든 플레이어에게 전송되어야 함
        Body body = new UseFieldItemBody(UseFieldItemBody.Result.SUCCESS, fieldItem.id);
        Packet packet = Packet.of(SendPacketType.USE_FIELD_ITEM, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 필드 아이템 사용 성공: {}번 필드 아이템 사용", event.channel().id(), room.id, fieldItem.id);
    }

    /**
     * 자가 제세동기 사용
     * 기절한 상태에서 자가 제세동기를 사용해 부활한다
     */
    public void on(UseDefibrillatorTickEvent event) {
        // 몽깅이 존재 확인
        if (!(players.getOrDefault(ChannelManager.getMemberId(event.channel()), null) instanceof Mongging mongging)) {
            Body body = new UseDefibrillatorBody(UseDefibrillatorBody.Result.NOT_FOUND_MONGGING, -1);
            Packet packet = Packet.of(SendPacketType.USE_DEFIBRILLATOR, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 제세동기 사용 실패: 요청자 {}번 사용자가 없거나 몽깅이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        int result = mongging.useDefibrillator();

        if (result == -1) {
            Body body = new UseDefibrillatorBody(UseDefibrillatorBody.Result.NOT_KNOCK_OUT, -1);
            Packet packet = Packet.of(SendPacketType.USE_DEFIBRILLATOR, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 제세동기 사용 실패: {}번 몽깅이가 기절 상태가 아님", event.channel().id(), room.id, mongging.getId());
            return;
        }

        if (result == -2) {
            Body body = new UseDefibrillatorBody(UseDefibrillatorBody.Result.NOT_FOUND_DEFIBRILLATOR, -1);
            Packet packet = Packet.of(SendPacketType.USE_DEFIBRILLATOR, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 제세동기 사용 실패: {}번 몽깅이에게 제세동기가 없음", event.channel().id(), room.id, mongging.getId());
            return;
        }

        Body body = new UseDefibrillatorBody(UseDefibrillatorBody.Result.SUCCESS, result);
        Packet packet = Packet.of(SendPacketType.USE_DEFIBRILLATOR, System.currentTimeMillis(), body);
        event.channel().write(packet);

        body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.error("[{} - {}] 제세동기 사용 성공: {}번 몽깅이가 제세동기로 부활함", event.channel().id(), room.id, mongging.getId());
    }

    /**
     * 상자 열기
     * 상자의 데이터를 사용자에게 반환하고, 상자를 보고 있는 사용자 정보에 요청한 사용자를 추가한다
     **/
    public void on(ShowBoxTickEvent event) {
        // 상자가 존재하지 않으면 실패
        if (!boxes.containsKey(event.command().boxId())) {
            Body body = new ShowBoxBody(false, -1, null);
            Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자 오픈 실패: {}번 상자가 없음", event.channel().id(), room.id, event.command().boxId());
            return;
        }

        // 요청 플레이어가 몽깅이가 아니면 실패
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (!(player instanceof Mongging mongging)) {
            Body body = new ShowBoxBody(false, -1, null);
            Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자 오픈 실패: 요청자 {}번 사용자가 몽깅이가 아님", event.channel().id(), room.id, player.getId());
            return;
        }

        // 상자 가져오기
        Box box = boxes.get(event.command().boxId());

        // 상자 근처에 없으면 실패
        if (!box.detectPosition(mongging.getPositionAt(System.currentTimeMillis()))) {
            Body body = new ShowBoxBody(false, -1, null);
            Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자 오픈 실패: 상자 근처에 없음", event.channel().id(), room.id);
            return;
        }

        // 상자 아이템 가져오기
        Boxable[] items = box.getItems();

        // 상자를 보고있는 세션 추가
        box.addViewer(player);

        // 상자 열기 응답 전송
        Body body = new ShowBoxBody(true, box.id, items);
        Packet packet = Packet.of(SendPacketType.SHOW_BOX, System.currentTimeMillis(), body);
        event.channel().write(packet);

        log.info("[{} - {}] 상자 오픈 성공: {}번 상자에 {}번 세션 추가", event.channel().id(), room.id, box.id, player.getId());
    }

    public void on(CloseBoxTickEvent event) {
        // 상자 존재 확인
        Box box = boxes.getOrDefault(event.command().boxId(), null);

        if (box == null) {
            log.error("[{} - {}] 상자 닫기 실패: {}번 상자가 없음", event.channel().id(), room.id, event.command().boxId());
            return;
        }

        // 상자를 보고있는 세션 삭제
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        boolean success = box.removeViewer(player);

        // 상자 닫기 응답 전송
        Body body = new CloseBoxBody(success ? CloseBoxBody.CloseResult.SUCCESS : CloseBoxBody.CloseResult.NOT_VIEWER);
        Packet packet = Packet.of(SendPacketType.CLOSE_BOX, System.currentTimeMillis(), body);
        event.channel().write(packet);

        log.info("[{} - {}] 상자 닫기 성공: {}번 상자에 {}번 플레이어 삭제", event.channel().id(), room.id, box.id, player.getId());
    }

    public void on(TakeItemTickEvent event) {
        // 상자 존재 확인
        Box box = boxes.getOrDefault(event.command().boxId(), null);
        if (box == null) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_BOX, ChannelManager.getMemberId(event.channel()), event.command().boxId(), null, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 상자가 없음", event.channel().id(), room.id, event.command().boxId());
            return;
        }

        // 인덱스 범위 확인
        if (event.command().index() < 0 || event.command().index() >= Box.BOX_SIZE) {
            Body body = new TakeItemBody(TakeItemBody.Result.INDEX_OUT_OF_RANGE, ChannelManager.getMemberId(event.channel()), box.id, null,
                    null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: 인덱스 범위 오류 - {}", event.channel().id(), room.id, event.command().index());
            return;
        }

        // 플레이어 존재 확인
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (player == null) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_PLAYER, ChannelManager.getMemberId(event.channel()), box.id, null, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 상자에서 아이템 꺼내기 실패: 요청자 {}번 사용자가 없음",
                    event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        // 플레이어가 몽깅이가 아니면 실패
        if (!(player instanceof Mongging mongging)) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_MONGGING, player.getId(), box.id, null, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: 요청자 {}번 사용자가 몽깅이가 아님", event.channel().id(), room.id, player.getId());
            return;
        }

        // 상자 근처에 없으면 실패
        if (!box.detectPosition(mongging.getPositionAt(System.currentTimeMillis()))) {
            Body body = new TakeItemBody(TakeItemBody.Result.NOT_NEAR, player.getId(), box.id, null, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: 상자 근처에 없음", event.channel().id(), room.id);
            return;
        }

        // 아이템 이동 (상자와 몽깅이 동시에 접근 방지)
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));

        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                // 상자에서 아이템 가져오기
                Boxable targetItem = box.getItemAt(event.command().index());
                if (targetItem == null) {
                    Body body = new TakeItemBody(TakeItemBody.Result.NOT_FOUND_BOX, player.getId(), box.id, null, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    log.warn("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 상자의 {}번째 칸에 아이템이 없음", event.channel().id(), room.id, box.id, event.command().index());
                    return;
                }

                // 몽깅이가 아이템을 가질 수 있는지 확인
                if (!mongging.canAddItem(targetItem)) {
                    Body body = new TakeItemBody(TakeItemBody.Result.FULL_ABOUT_ITEM, player.getId(), box.id, null, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    log.info("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 몽깅이의 인벤토리에 빈 공간이 없음", event.channel().id(), room.id, player.getId());
                    return;
                }

                // 아이템 꺼내기
                Boxable[] popResult = box.popItem(event.command().index());
                boolean success = mongging.addItem(popResult[Box.BOX_SIZE]);

                // 아이템 꺼내기 실패 시 실패 응답 전송
                if (!success) {
                    log.error("[{} - {}] 상자에서 아이템 꺼내기 실패: {}번 몽깅이가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패. 인벤토리: {}",
                            event.channel().id(), room.id,
                            player.getId(), targetItem, popResult[Box.BOX_SIZE],
                            Arrays.deepToString(mongging.getItems()));

                    Body body = new TakeItemBody(TakeItemBody.Result.FAIL, player.getId(), box.id, null, null);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);
                    return;
                }

                // 아이템 꺼내기 성공 시 성공 응답 전송
                box.getViewers().forEach(viewer -> {
                    Body body = new TakeItemBody(TakeItemBody.Result.SUCCESS, viewer.getId(), box.id, popResult, popResult[Box.BOX_SIZE]);
                    Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
                    this.room.sendPacket(viewer.getId(), packet);
                });

                log.info("[{} - {}] 상자에서 아이템 꺼내기 성공: {}번 몽깅이가 {} 아이템 획득", event.channel().id(), room.id, player.getId(), popResult[Box.BOX_SIZE]);
            }
        }
    }

    public void on(PutItemTickEvent event) {
        // 아이템 존재 확인
        Boxable targetItem = ItemDictionary.valueOf(event.command().itemId());
        if (targetItem == null) {
            Body body = new PutItemBody(PutItemBody.Result.ILLEGAL_ITEM_ID, null);
            Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 아이디에 대응하는 아이템 없음", event.channel().id(), room.id, event.command().itemId());
            return;
        }

        // 상자 존재 확인
        Box box = boxes.getOrDefault(event.command().boxId(), null);
        if (box == null) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_BOX, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 아이디에 대응하는 상자 없음", event.channel().id(), room.id, event.command().boxId());
            return;
        }

        // 플레이어 존재 확인
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (player == null) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_PLAYER, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 상자에 아이템 넣기 실패: 요청자 {}번 사용자가 없음", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        // 플레이어가 몽깅이가 아니면 실패
        if (!(player instanceof Mongging mongging)) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_MONGGING, null);
            Packet packet = Packet.of(SendPacketType.TAKE_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 상자에 아이템 넣기 실패: 요청자 {}번 사용자가 몽깅이가 아님", event.channel().id(), room.id, player.getId());
            return;
        }

        // 상자 근처에 없으면 실패
        if (!box.detectPosition(mongging.getPositionAt(System.currentTimeMillis()))) {
            Body body = new PutItemBody(PutItemBody.Result.NOT_NEAR, null);
            Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 상자에 아이템 넣기 실패: 상자 근처에 없음", event.channel().id(), room.id);
            return;
        }

        // 상자와 몽깅이 동시에 접근 방지
        List<Object> lockOrder = Arrays.asList(box, mongging);
        lockOrder.sort(Comparator.comparing(Object::hashCode));
        synchronized (lockOrder.get(0)) {
            synchronized (lockOrder.get(1)) {
                // 몽깅이에게 아이템이 없으면 실패
                int count = mongging.countItem(targetItem);
                if (count == 0) {
                    Body body = new PutItemBody(PutItemBody.Result.NOT_FOUND_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    log.warn("[{} - {}] 상자에 아이템 넣기 실패: {}번 몽깅이에게 {}번 아이템이 없음", event.channel().id(), room.id, player.getId(), targetItem.id);
                    return;
                }

                // 상자가 가득 차있으면 실패
                if (box.isFull()) {
                    Body body = new PutItemBody(PutItemBody.Result.FULL_ABOUT_ITEM, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    log.info("[{} - {}] 상자에 아이템 넣기 실패: {}번 상자에 빈 공간이 없음", event.channel().id(), room.id, box.id);
                    return;
                }

                // 아이템 넣기
                Boxable boxable = mongging.popItem(targetItem);
                boolean success = box.addItem(boxable);

                // 아이템 넣기 실패 시 실패 응답 전송
                if (!success) {
                    Body body = new PutItemBody(PutItemBody.Result.FAIL, null);
                    Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                    event.channel().write(packet);

                    log.error("[{} - {}] 상자에 아이템 넣기 실패: {}번 상자가 {}을 가질 수 있는지 확인하고 {}을 넣었는 데 실패. 상자: {}",
                            event.channel().id(),
                            room.id,
                            box.id,
                            targetItem,
                            boxable,
                            Arrays.deepToString(mongging.getItems()));
                    return;
                }

                // 아이템 넣기 성공 시 성공 응답 전송
                Body body = new PutItemBody(PutItemBody.Result.SUCCESS, box.getItems());
                Packet packet = Packet.of(SendPacketType.PUT_ITEM, System.currentTimeMillis(), body);
                event.channel().write(packet);

                log.info("[{} - {}] 상자에 아이템 넣기 성공: {}번 상자에 {} 아이템 추가", event.channel().id(), room.id, box.id, targetItem.id);
                return;
            }
        }
    }

    public void on(DigUpTickEvent event) {
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (!(player instanceof Mongging mongging)) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.NOT_FOUND_MONGGING);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 파기 시작 실패: {}번 플레이어가 없거나 몽깅이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        if (!ggumtles.containsKey(event.command().ggumtleId())) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.NOT_FOUND_GGUMTLE);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 파기 시작 실패: {}번 아이디에 대응하는 꿈틀이가 없음", event.channel().id(), room.id, event.command().ggumtleId());
            return;
        }

        Ggumtle ggumtle = ggumtles.get(event.command().ggumtleId());
        if (ggumtle.isDugUp()) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.ALREADY_DIG_UP);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 꿈틀이 파기 시작 실패: {}번 꿈틀이가 이미 파짐", event.channel().id(), room.id, ggumtle.id);
            return;
        }

        Position playerPosition = mongging.getPositionAt(System.currentTimeMillis());
        if (!ggumtle.detectDigUp(playerPosition)) {
            Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.NOT_AROUND);
            Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 꿈틀이 파기 시작 실패: {}번 사용자가 {}번 꿈틀이의 유효 범위 내에 없음", event.channel().id(), room.id, player.getId(), ggumtle.id);
            return;
        }

        ScheduledFuture<?> future = workerThreadPool.schedule(() -> {
            Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
            while (iterator.hasNext()) {
                Map.Entry<Long, WorkingThread> entry = iterator.next();
                if (entry.getValue().threadType == WorkingThread.ThreadType.DIG_UP
                        && entry.getValue().ggumtleId == ggumtle.id) {
                    entry.getValue().scheduledFuture.cancel(true);
                    iterator.remove();
                }
            }

            int digUpResult = ggumtle.tryDigUp();

            // 3초를 기다리는 동안 누군가 파냈으면 무시
            if (digUpResult == 0) {
                log.warn("[{} - {}] 꿈틀이 파기 무시: 해당 작업이 기다리는 동안 {}번 꿈틀이가 파져서 무시", event.channel().id(), room.id, ggumtle.id);
                return;
            }

            log.info("[{} - {}] 꿈틀이 파기 완료: {}번 꿈틀이 파기 완료", event.channel().id(), room.id, ggumtle.id);

            // 꿈틀이에 따른 브로드캐스팅
            if (digUpResult == 1) {
                Body body = new GgumtleStatusBody(ggumtle.id, GgumtleStatusBody.Status.NORMAL);
                Packet packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                // 몽깅이 상태를 NORMAL로 복원
                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                log.info("[{} - {}] 꿈틀이 파기 성공: {}번 몽깅이가 {}번 꿈틀이를 성공적으로 파내고 상태가 NORMAL로 복원됨", event.channel().id(), room.id, player.getId(), ggumtle.id);
            }

            if (digUpResult == -1) {
                Body body = new GgumtleStatusBody(ggumtle.id, GgumtleStatusBody.Status.FAKE);
                Packet packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.STUNNED);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                log.info("[{} - {}] 꿈틀이 파기 스턴: 가짜 꿈틀이를 파낸 {}번 몽깅이 스턴", event.channel().id(), room.id, player.getId());

                // 1.5초 후 스턴 상태 해제
                ScheduledFuture<?> stunRecoveryFuture = workerThreadPool.schedule(() -> {
                    Body stunRecoveryBody = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                    Packet stunRecoveryPacket = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), stunRecoveryBody);
                    this.room.broadcast(stunRecoveryPacket);

                    log.info("[{} - {}] 스턴 상태 해제: {}번 몽깅이의 스턴 상태가 자동으로 해제됨", event.channel().id(), room.id, player.getId());
                }, 1500, TimeUnit.MILLISECONDS);
            }
        }, 3, TimeUnit.SECONDS);
        workingThreads.put(player.getId(), new WorkingThread(player.getId(), future, WorkingThread.ThreadType.DIG_UP, ggumtle.id));

        // 꿈틀이 파기 시작 성공 응답
        Body body = new DigUpReceiveBody(DigUpReceiveBody.Result.START_DIGGING);
        Packet packet = Packet.of(SendPacketType.DIG_UP_RECEIVE, System.currentTimeMillis(), body);
        event.channel().write(packet);

        // 몽깅이 땅파는 상태 전파
        body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.DIGGING);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        // 다른 몽깅이가 해당 꿈틀이에 작업 중이었는지 확인
        long count = workingThreads.values().stream()
                .filter(thread -> thread.playerId != player.getId() && thread.ggumtleId == ggumtle.id)
                .count();
        if (count == 0) {
            body = new GgumtleStatusBody(ggumtle.id, GgumtleStatusBody.Status.DIGGING);
            packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);
            log.info("[{} - {}] 꿈틀이 파기 시작 방송: {}번 꿈틀이의 상태를 파는 중으로 방송", event.channel().id(), room.id, ggumtle.id);
        }

        log.info("[{} - {}] 꿈틀이 파기 시작 완료: 잠시 후 {}번 꿈틀이 파기 완료 예정", event.channel().id(), room.id, ggumtle.id);
    }

    public void on(StopDiggingTickEvent event) {
        Long memberId = ChannelManager.getMemberId(event.channel());

        WorkingThread targetThread = workingThreads.getOrDefault(memberId, null);

        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.DIG_UP) {
            targetThread.scheduledFuture.cancel(true);
            workingThreads.remove(memberId);

            Body body = new StopDiggingBody(StopDiggingBody.Result.STOP);
            Packet packet = Packet.of(SendPacketType.STOP_DIGGING, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.info("[{} - {}] 꿈틀이 파기 중단 완료: {}번 사용자의 작업 중단", event.channel().id(), room.id, memberId);

            // 몽깅이 상태를 NORMAL로 복원
            Player player = players.getOrDefault(memberId, null);
            if (player instanceof Mongging mongging) {
                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                log.info("[{} - {}] 꿈틀이 파기 중단: {}번 몽깅이 상태가 NORMAL로 복원됨", event.channel().id(), room.id, memberId);
            }

            // 다른 몽깅이가 해당 꿈틀이에 작업 중이 아니면, 묻힘 상태 전파
            long count = workingThreads.values().stream()
                    .filter(thread -> thread.ggumtleId == targetThread.ggumtleId)
                    .count();
            if (count == 0 && !ggumtles.get(targetThread.ggumtleId).isDugUp()) {
                body = new GgumtleStatusBody(targetThread.ggumtleId, GgumtleStatusBody.Status.BURY);
                packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);
                log.info("[{} - {}] 꿈틀이 파기 중단 전파: {}번 꿈틀이에 작업 중인 사용자가 없어 묻힘 상태로 전파", event.channel().id(), room.id, memberId);
            }
        } else {
            Body body = new StopDiggingBody(StopDiggingBody.Result.NOT_FOUND_DIGGING);
            Packet packet = Packet.of(SendPacketType.STOP_DIGGING, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 꿈틀이 파기 중단 실패: {}번 사용자가 꿈틀이 파내기 작업 없음", event.channel().id(), room.id, memberId);
        }
    }

    public void on(StartFeedTickEvent event) {
        Player player = players.getOrDefault(ChannelManager.getMemberId(event.channel()), null);
        if (!(player instanceof Mongging mongging)) {
            Body body = new StartFeedBody(StartFeedBody.Result.NOT_FOUND_PLAYER);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 플레이어가 없거나 몽깅이가 아님", event.channel().id(), room.id, player.getId());
            return;
        }

        Ggumtle targetGgumtle = ggumtles.get(event.command().ggumtleId());
        if (!targetGgumtle.isDugUp()) {
            Body body = new StartFeedBody(StartFeedBody.Result.YET_DIG_UP);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 꿈틀이가 아직 파지지 않음", event.channel().id(), room.id, targetGgumtle.id);
            return;
        }

        if (targetGgumtle.isDone()) {
            Body body = new StartFeedBody(StartFeedBody.Result.ALREADY_DONE);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 꿈틀이가 이미 정화됨", event.channel().id(), room.id, targetGgumtle.id);
            return;
        }

        int count = mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem);
        if (count == 0) {
            Body body = new StartFeedBody(StartFeedBody.Result.LACK_OF_FEED_ITEM);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 몽깅이에게 빛젤리가 없음", event.channel().id(), room.id, player.getId());
            return;
        }

        Position position = mongging.getPositionAt(System.currentTimeMillis());
        if (!targetGgumtle.detectFeed(position)) {
            Body body = new StartFeedBody(StartFeedBody.Result.NOT_AROUND);
            Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.warn("[{} - {}] 꿈틀이 먹이기 시작 실패: {}번 몽깅이가 {}번 꿈틀이 근처에 없음", event.channel().id(), room.id, player.getId(), targetGgumtle.id);
            return;
        }

        Runnable task = () -> {
            final int leftNeedJellyCount = targetGgumtle.feed();
            // 성불 및 종료
            if (leftNeedJellyCount == 0) {
                mongging.popItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                final int leftLightJellyCount = mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem);

                Body body = new LeftJellyCountBody(leftLightJellyCount);
                Packet packet = Packet.of(SendPacketType.LEFT_JELLY_COUNT, System.currentTimeMillis(), body);
                event.channel().write(packet);

                body = new GgumtleFedJellyCountBody(targetGgumtle.id, Ggumtle.INIT_LEFT_FEED_COUNT - leftNeedJellyCount);
                packet = Packet.of(SendPacketType.GGUMTLE_FED_JELLY, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                Iterator<Map.Entry<Long, WorkingThread>> iterator = workingThreads.entrySet().iterator();
                while (iterator.hasNext()) {
                    Map.Entry<Long, WorkingThread> entry = iterator.next();
                    if (entry.getValue().ggumtleId == targetGgumtle.id) {
                        entry.getValue().scheduledFuture.cancel(true);
                        iterator.remove();
                    }
                }

                body = new StopFeedingBody(StopFeedingBody.Result.STOP, mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
                event.channel().write(packet);

                // 몽깅이 상태를 NORMAL로 복원
                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                body = new GgumtleStatusBody(targetGgumtle.id, GgumtleStatusBody.Status.DONE);
                packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                tryOpenExit();

                log.info("[{} - {}] 꿈틀이 먹이기 완료: 꿈틀이가 정화해 종료, {}번 몽깅이 상태가 NORMAL로 복원됨", event.channel().id(), room.id, player.getId());
                return;
            }

            // 다른 몽깅이가 먼저 성불시킴
            if (leftNeedJellyCount < 0) {
                WorkingThread removedThread = workingThreads.remove(player.getId());
                removedThread.scheduledFuture.cancel(true);

                Body body = new StopFeedingBody(StopFeedingBody.Result.STOP, mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
                event.channel().write(packet);

                // 몽깅이 상태를 NORMAL로 복원
                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                log.info("[{} - {}] 꿈틀이 먹이기 종료: 다른 몽깅이가 성불시켜 종료, {}번 몽깅이 상태가 NORMAL로 복원됨", event.channel().id(), room.id, player.getId());
                return;
            }

            mongging.popItem(ItemDictionary.LIGHT_JELLY.boxableItem);
            final int leftLightJellyCount = mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem);

            Body body = new LeftJellyCountBody(leftLightJellyCount);
            Packet packet = Packet.of(SendPacketType.LEFT_JELLY_COUNT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            body = new GgumtleFedJellyCountBody(targetGgumtle.id, Ggumtle.INIT_LEFT_FEED_COUNT - leftNeedJellyCount);
            packet = Packet.of(SendPacketType.GGUMTLE_FED_JELLY, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            // 남은 아이템이 없으면 종료
            if (leftLightJellyCount == 0) {
                WorkingThread removedThread = workingThreads.remove(player.getId());
                removedThread.scheduledFuture.cancel(true);

                body = new StopFeedingBody(StopFeedingBody.Result.STOP, leftLightJellyCount);
                packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
                event.channel().write(packet);

                body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
                packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);

                log.info("[{} - {}] 아이템 소진으로 먹이기 종료: {}번 몽깅이 상태가 NORMAL로 복원됨", event.channel().id(), room.id, player.getId());

                // 작업 중인 몽깅이가 없으면 일반 상태로 전파
                boolean isWorking = workingThreads.values().stream()
                        .anyMatch(thread -> thread.ggumtleId == targetGgumtle.id);
                if (!isWorking) {
                    body = new GgumtleStatusBody(targetGgumtle.id, GgumtleStatusBody.Status.NORMAL);
                    packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                    this.room.broadcast(packet);
                    log.info("[{} - {}] 꿈틀이 먹이기 전파: {}번 꿈틀이에 작업 중인 몽깅이가 없어 일반 상태로 전파", event.channel().id(), room.id, targetGgumtle.id);
                }

                log.info("[{} - {}] 꿈틀이 먹이기 완료: 아이템을 모두 소진해 종료", event.channel().id(), room.id);
                return;
            }
        };
        ScheduledFuture<?> future = workerThreadPool.scheduleAtFixedRate(task, 1, 1, TimeUnit.SECONDS);
        workingThreads.put(player.getId(), new WorkingThread(player.getId(), future, WorkingThread.ThreadType.FEED, targetGgumtle.id));

        Body body = new StartFeedBody(StartFeedBody.Result.START_FEEDING);
        Packet packet = Packet.of(SendPacketType.START_FEED, System.currentTimeMillis(), body);
        event.channel().write(packet);

        body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.FEEDING);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        // 다른 사용자가 작업중이 아니면 Feeding으로 전파
        boolean isWorking = workingThreads.values().stream()
                .anyMatch(thread -> thread.ggumtleId == targetGgumtle.id && thread.playerId != player.getId());
        if (!isWorking) {
            body = new GgumtleStatusBody(targetGgumtle.id, GgumtleStatusBody.Status.FEEDING);
            packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 꿈틀이 먹이기 시작 전파: {}번 사용자가 {}번 꿈틀이에게 빛젤리 먹이기 전파", event.channel().id(), room.id, player.getId(), targetGgumtle.id);
        }

        log.info("[{} - {}] 꿈틀이 먹이기 시작 성공: {}번 사용자가 {}번 꿈틀이에게 빛젤리 먹이기 시작함", event.channel().id(), room.id, player.getId(), targetGgumtle.id);
    }

    public void on(StopFeedingTickEvent event) {
        Long memberId = ChannelManager.getMemberId(event.channel());

        WorkingThread targetThread = workingThreads.getOrDefault(memberId, null);

        Mongging mongging = (Mongging) players.get(memberId);
        int leftLightJellyCount = mongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem);

        if (targetThread != null && targetThread.threadType == WorkingThread.ThreadType.FEED) {
            targetThread.scheduledFuture.cancel(true);
            workingThreads.remove(memberId);

            Body body = new StopFeedingBody(StopFeedingBody.Result.STOP, leftLightJellyCount);
            Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.info("[{} - {}] 꿈틀이 먹이기 중단 성공: {}번 사용자의 빛젤리 먹이기 작업 중단", event.channel().id(), room.id, memberId);

            body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.NORMAL);
            packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
            this.room.broadcast(packet);

            log.info("[{} - {}] 꿈틀이 먹이기 중단: {}번 몽깅이 상태가 NORMAL로 복원됨", event.channel().id(), room.id, memberId);

            // 다른 몽깅이가 해당 꿈틀이에 작업 중이 아니면, 일반 상태 전파
            long count = workingThreads.values().stream()
                    .filter(thread -> thread.ggumtleId == targetThread.ggumtleId)
                    .count();
            if (count == 0 && !ggumtles.get(targetThread.ggumtleId).isDone()) {
                body = new GgumtleStatusBody(targetThread.ggumtleId, GgumtleStatusBody.Status.NORMAL);
                packet = Packet.of(SendPacketType.GGUMTLE_STATUS, System.currentTimeMillis(), body);
                this.room.broadcast(packet);
                log.info("[{} - {}] 꿈틀이 먹이기 중단 전파: {}번 꿈틀이에 작업 중인 사용자가 없어 일반 상태로 전파", event.channel().id(), room.id, memberId);
            }
        } else {
            Body body = new StopFeedingBody(StopFeedingBody.Result.NOT_FOUND, leftLightJellyCount);
            Packet packet = Packet.of(SendPacketType.STOP_FEED, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 꿈틀이 먹이기 종료 실패: {}번 사용자에게 빛젤리 먹이기 작업 없음", event.channel().id(), room.id, memberId);
        }
    }

    public void tryOpenExit() {
        for (Ggumtle ggumtle : ggumtles.values()) {
            if (!(ggumtle instanceof FakeGgumtle) && !ggumtle.isDone()) {
                log.info("{}번 드림에 아직 정화되지 않은 꿈틀이가 있어 탈출구가 열리지 않음", this.room.id);
                return;
            }
        }

        boolean isExpected = isExitOpen.compareAndSet(false, true);
        if (!isExpected) {
            log.warn("{}번 드림의 탈출구 오픈이 다시 이루어졌습니다", this.room.id);
            return;
        }

        Body body = new ExitOpen(this.exits.values().stream().toList());
        Packet packet = Packet.of(SendPacketType.OPEN_EXIT, System.currentTimeMillis(), body);
        this.room.broadcast(packet);
        log.info("{}번 드림에 탈출구가 열림!", this.room.id);
    }

    public void on(EscapeTickEvent event) {
        Exit exit = exits.getOrDefault(event.command().exitId(), null);

        // 탈출구 존재 확인
        if (exit == null) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_FOUND_EXIT);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: {}번 아이디에 해당하는 문이 없음", event.channel().id(), room.id, event.command().exitId());
            return;
        }

        // 몽깅이 존재 확인
        Player player = players.get(ChannelManager.getMemberId(event.channel()));
        if (!(player instanceof Mongging mongging)) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_MONGGING);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: 요청 {}번 사용자는 몽깅이가 아님", event.channel().id(), room.id, ChannelManager.getMemberId(event.channel()));
            return;
        }

        // 몽깅이 위치가 탈출구 범위 내에 있는지 확인
        Position monggingPosition = mongging.getPositionAt(System.currentTimeMillis());
        if (!exit.detectEscape(monggingPosition)) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_IN_EXIT);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: {}번 몽깅이가 탈출구 범위 내에 없음", event.channel().id(), room.id, player.getId());
            return;
        }

        // 몽깅이 생존 확인
        if (!mongging.isNotDead()) {
            Body body = new EscapeBody(EscapeBody.Result.NOT_ALIVE);
            Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
            event.channel().write(packet);

            log.error("[{} - {}] 몽깅이 탈출 실패: {}번 몽깅이는 죽어서 탈출이 불가능", event.channel().id(), room.id, player.getId());
            return;
        }

        // 몽깅이 탈출
        mongging.escape();

        // 몽깅이 탈출 성공
        Body body = new EscapeBody(EscapeBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.ESCAPE_RESULT, System.currentTimeMillis(), body);
        event.channel().write(packet);

        body = new MonggingStatusBody(mongging.getId(), MonggingStatusBody.Result.ESCAPE);
        packet = Packet.of(SendPacketType.MONGGING_STATUS, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽깅이 탈출 성공: {}번 몽깅이 탈출 성공", event.channel().id(), room.id, player.getId());

        // 몽깅이 승리 조건 확인
        long escapedMonggingCount = players.values().stream()
                .filter(p -> p instanceof Mongging m && m.isEscaped())
                .count();
        if (escapedMonggingCount >= WINNING_MONGGING_COUNT) {
            endDream(true);
            log.info("[{} - {}] 몽깅이 승리: 몽깅이가 탈출 조건보다 많이 탈출하여 승리", event.channel().id(), room.id);
        }
    }

    private void makeScare(Mongdung mongdung, Channel channel) {
        boolean success = mongdung.scare();

        if (!success) {
            Body body = new MongdungSkillBody(Mongdung.SkillType.SCARE, MongdungSkillBody.Result.YET_COOL_TIME);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            channel.write(packet);

            log.warn("[{} - {}] 몽둥이 공포 스킬 실패: 쿨타임 부족", channel.id(), room.id);
            return;
        }

        Body body = new MongdungSkillBody(Mongdung.SkillType.SCARE, MongdungSkillBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽둥이 공포 스킬 성공", channel.id(), room.id);
    }

    private void buryFakeGgumtle(Mongdung mongdung, int x, int y, int z, Channel channel) {
        boolean success = mongdung.tryBuryFakeGgumtle();

        if (!success) {
            Body body = new MongdungSkillBody(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillBody.Result.LACK_USE_COUNT);
            Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
            channel.write(packet);

            log.warn("[{} - {}] 몽둥이 가짜 꿈틀이 스킬 실패: 사용 횟수 소진", channel.id(), room.id);
            return;
        }

        Body body = new MongdungSkillBody(Mongdung.SkillType.FAKE_GGUMTLE, MongdungSkillBody.Result.SUCCESS);
        Packet packet = Packet.of(SendPacketType.MONGDUNG_SKILL, System.currentTimeMillis(), body);
        channel.write(packet);

        int id = ++ggumtleIdGenerator;
        FakeGgumtle fakeGgumtle = new FakeGgumtle(id, new Position(x, y, z, -1));

        this.ggumtles.put(id, fakeGgumtle);

        body = new NewGgumtleBody(fakeGgumtle.id, fakeGgumtle.position);
        packet = Packet.of(SendPacketType.NEW_GGUMTLE, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("[{} - {}] 몽둥이 가짜 꿈틀이 스킬 성공", channel.id(), room.id);
    }

    private void distributeDroppedItem(Mongging mongging) {
        Map<Boxable, Integer> droppedItems = mongging.getDroppedItems();
        for (Map.Entry<Boxable, Integer> entry : droppedItems.entrySet()) {
            ItemDistributor.distribute(entry.getKey(), boxes.values().stream().toList(), entry.getValue());
        }

        log.info("[{} - {}] 기절 및 사망한 몽깅이의 아이템 재공급: {}", null, room.id, droppedItems);
    }

    private void endDream(boolean isMonggingWin) {
        // 자원 정리
        shutdownThread();

        // 드림 종료 브로드캐스팅
        Body body = new DreamEndBody(isMonggingWin, this.players.values());
        Packet packet = Packet.of(SendPacketType.END, System.currentTimeMillis(), body);
        this.room.broadcast(packet);

        log.info("{}번 드림 종료: 몽깅이 우승 = {}", room.id, isMonggingWin);

        // 드림 종료 이벤트 발행
        applicationEventPublisher.publishEvent(DreamEndEvent.of(this.room.id, players.values(), isMonggingWin));
    }

    private void shutdownThread() {
        // 스레드풀 정리
        workerThreadPool.shutdownNow();
        timerThread.shutdownNow();

        // 작업 중인 스레드 정리
        for (Map.Entry<Long, WorkingThread> entry : workingThreads.entrySet()) {
            try {
                entry.getValue().scheduledFuture.cancel(true);
            } catch (Exception e) {
                log.error("작업 스레드 종료에 실패했습니다: {}", entry.getValue());
            }
        }
        workingThreads.clear();

        // 타이머 스레드 정리
        try {
            timerFuture.cancel(true);
        } catch (Exception e) {
            log.error("타이머 스레드 종료에 실패했습니다");
        }
    }

    private void initializeMap() {
        List<GgumtleSpawn> ggumtleSpawns;
        List<BoxSpawn> boxSpawns;
        List<FieldItemSpawn> fieldItemSpawns;

        // TEST
        if (this.room.id < 0) {
            ggumtleSpawns = new ArrayList<>();
            ggumtleSpawns.add(new GgumtleSpawn(0, 4399, 499, -602));
            ggumtleSpawns.add(new GgumtleSpawn(0, 5982, 469, 763));
            ggumtleSpawns.add(new GgumtleSpawn(0, 3920, 499, 543));

            boxSpawns = new ArrayList<>();
            boxSpawns.add(new BoxSpawn(0, 3468, 499, -24));
            boxSpawns.add(new BoxSpawn(0, 38460, 515, 135));
            boxSpawns.add(new BoxSpawn(0, 4449, 499, 211));
            // boxSpawns.add(new BoxSpawn(0, 3981, 500, -497));
            boxSpawns.add(new BoxSpawn(0, 5548, 491, -96));
            boxSpawns.add(new BoxSpawn(0, 6403, 459, 365));
            boxSpawns.add(new BoxSpawn(0, 5622, 479, 718));
            // boxSpawns.add(new BoxSpawn(0, 5075, 496, 1209));
            // boxSpawns.add(new BoxSpawn(0, 5810, 482, 1043));
            // boxSpawns.add(new BoxSpawn(0, 5943, 466, 451));
            boxSpawns.add(new BoxSpawn(0, 4608, 499, -36));
            // boxSpawns.add(new BoxSpawn(0, 4301, 499, -204));
            boxSpawns.add(new BoxSpawn(0, 5502, 499, -543));
            // boxSpawns.add(new BoxSpawn(0, 4331, 500, -883));
            // boxSpawns.add(new BoxSpawn(0, 6028, 513, -539));
            boxSpawns.add(new BoxSpawn(0, 4316, 550, -3577));
            boxSpawns.add(new BoxSpawn(0, 4141, 604, -3331));
            boxSpawns.add(new BoxSpawn(0, 4030, 624, -3669));
            boxSpawns.add(new BoxSpawn(0, 4253, 571, -3839));
            boxSpawns.add(new BoxSpawn(0, 4459, 528, -3781));
            boxSpawns.add(new BoxSpawn(0, 4547, 513, -3614));

            fieldItemSpawns = new ArrayList<>();
            fieldItemSpawns.add(new FieldItemSpawn(2, 5466, 609, 2233, 1));
            fieldItemSpawns.add(new FieldItemSpawn(0, 6295, 537, -236, 2));
            fieldItemSpawns.add(new FieldItemSpawn(1, 4041, 538, -111, 2));
            log.info("테스트용 맵 생성");
        } else {
            ggumtleSpawns = spawnCache.getRandomGgumtleSpawns(GGUMTLE_SPAWN_SIZE);
            boxSpawns = spawnCache.getRandomBoxSpawns(BOX_SPAWN_SIZE);
            fieldItemSpawns = spawnCache.getFieldItemSpawns();
            log.info("무작의 맵 생성");
        }

        // 꿈틀이 위치 초기화
        for (int i = 0; i < GGUMTLE_SPAWN_SIZE; i++) {
            ggumtles.put(++ggumtleIdGenerator, new Ggumtle(ggumtleIdGenerator, Position.from(ggumtleSpawns.get(i))));
        }

        // TEST: 가짜 꿈틀이
        if (this.room.id < 0) {
            int id = ++ggumtleIdGenerator;
            Position fakePosition = new Position(
                    (int) (52.38745 * 100),
                    (int) (5.027 * 100),
                    (int) (14.37919 * 100),
                    0);
            FakeGgumtle fakeGgumtle = new FakeGgumtle(id, fakePosition);
            ggumtles.put(id, fakeGgumtle);
        }

        // 상자 위치 초기화
        for (int i = 0; i < boxSpawns.size(); i++) {
            boxes.put(i, new Box(i, Position.from(boxSpawns.get(i))));
        }

        // 상자 아이템 초기화
        List<Box> boxes = this.boxes.values().stream().toList();
        if (this.room.id < 0) {
            boxes.getFirst().addItem(ItemDictionary.TASER.boxableItem);
            boxes.getFirst().addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
            boxes.getFirst().addItem(ItemDictionary.DEFIBRILLATOR.boxableItem);
        }
        for (ItemDictionary itemDictionary : ItemDictionary.values()) {
            ItemDistributor.distribute(itemDictionary.boxableItem, boxes, itemDictionary.boxableItem.initialCount);
        }

        // 필드 아이템 초기화
        for (FieldItemSpawn spawn : fieldItemSpawns) {
            FieldItem fieldItem = new FieldItem(spawn);
            fieldItems.put(fieldItem.id, fieldItem);
        }

        // 출구 초기화
        List<ExitSpawn> exitSpawns = spawnCache.getExitSpawns();
        for (int i = 0; i < exitSpawns.size(); i++) {
            exits.put(i, new Exit(i, Position.from(exitSpawns.get(i))));
        }

        log.info("{}번 드림의 맵 초기화 종료", room.id);

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

        log.info("{}번 드림 맵 초기화 완료: 꿈틀이={}, 힐팩={}, 스피드팩={}, 출구={}, 상자={}", this.room.id, this.ggumtles, healPacks,
                speedPacks, this.exits, this.boxes);
    }

    // TODO: 클래스 별 체력, 속도 초기화
    private void initializePlayers() {
        List<Long> playerIds = room.getPlayerIds().stream().sorted().toList();
        List<PlayerSpawn> playerSpawns;

        // TEST, ,
        int mongdungIndex;
        if (this.room.id < 0) {
            mongdungIndex = 0;
            playerSpawns = List.of(
                    new PlayerSpawn(1, (int) (60.1313 * 100), (int) (4.520969 * 100), (int) (-2.583155 * 100)),
                    new PlayerSpawn(1, 45 * 100, 5 * 100, 2 * 100),
                    new PlayerSpawn(1, 47 * 100, 5 * 100, 4 * 100),
                    new PlayerSpawn(1, 47 * 100, 5 * 100, 0 * 100),
                    new PlayerSpawn(1, 47 * 100, 5 * 100, 6 * 100));
        } else {
            mongdungIndex = pickMongdungIndex(playerIds.size());
            playerSpawns = spawnCache.getRandomPlayerSpawns(playerIds.size());
        }

        // TEST
        if (this.room.id == -4) {
            Mongging mongging = new Mongging(playerIds.getFirst(), Position.from(playerSpawns.getFirst()),
                    this.room.getPlayerInfo(playerIds.getFirst()));
            for (int j = 0; j < 15; j++) {
                mongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
            }
            this.players.put(mongging.getId(), mongging);
        } else if (this.room.id == -5) {
            Mongdung mongdung = new Mongdung(playerIds.getFirst(), Position.from(playerSpawns.getFirst()));
            this.players.put(mongdung.getId(), mongdung);
        } else {
            this.players.clear();
            for (int i = 0; i < playerIds.size(); i++) {
                if (i == mongdungIndex) {
                    Mongdung mongdung = new Mongdung(playerIds.get(i), Position.from(playerSpawns.get(i)));
                    this.players.put(mongdung.getId(), mongdung);
                    continue;
                }
                Mongging mongging = new Mongging(
                        playerIds.get(i),
                        Position.from(playerSpawns.get(i)),
                        this.room.getPlayerInfo(playerIds.get(i))
                );
                this.players.put(mongging.getId(), mongging);

                // TEST
                if (this.room.id < 0) {
                    for (int j = 0; j < 15; j++) {
                        mongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                    }
                }
            }
        }

        log.info("{}번 드림의 플레이어 초기화 종료: {}", room.id, this.players.values());

        List<Player> players = this.players.values().stream().toList();
        for (long playerId : playerIds) {
            Body body = new InitializePlayerBody(playerId, players, this.room.getPlayerInfos());
            Packet packet = Packet.of(SendPacketType.INITIALIZE_PLAYER, System.currentTimeMillis(), body);
//            boolean success = room.sendPacket(playerId, packet);
        }
    }

    private int pickMongdungIndex(int size) {
        Random random = new Random();

        return random.nextInt(size);
    }
}
