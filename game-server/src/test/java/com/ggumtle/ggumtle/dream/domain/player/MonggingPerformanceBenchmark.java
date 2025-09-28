package com.ggumtle.ggumtle.dream.domain.player;

import com.ggumtle.ggumtle.dream.domain.item.ItemDictionary;
import com.ggumtle.ggumtle.dream.vo.Position;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;
import org.openjdk.jmh.annotations.*;
import org.openjdk.jmh.infra.Blackhole;
import org.openjdk.jmh.runner.Runner;
import org.openjdk.jmh.runner.RunnerException;
import org.openjdk.jmh.runner.options.Options;
import org.openjdk.jmh.runner.options.OptionsBuilder;

import java.util.concurrent.ThreadLocalRandom;
import java.util.concurrent.TimeUnit;

@BenchmarkMode(Mode.Throughput)
@OutputTimeUnit(TimeUnit.SECONDS)
@State(Scope.Benchmark)
@Threads(8)
@Warmup(iterations = 3, time = 1, timeUnit = TimeUnit.SECONDS)
@Measurement(iterations = 5, time = 2, timeUnit = TimeUnit.SECONDS)
@Fork(1)
public class MonggingPerformanceBenchmark {

    private Mongging lockBasedMongging;
    private MonggingSynchronized synchronizedMongging;
    private PlayerInfo playerInfo;

    @Setup(Level.Trial)
    public void setup() {
        playerInfo = PlayerInfo.builder()
                .playerId(1L)
                .nickname("testPlayer")
                .monggingClassId(1L)
                .additionalHp(0)
                .additionalHealSpeed(0)
                .additionalTaskSpeed(0)
                .build();

        Position position = new Position(0, 0, 0, System.currentTimeMillis());

        lockBasedMongging = new Mongging(1L, position, playerInfo);
        synchronizedMongging = new MonggingSynchronized(1L, position, playerInfo);
    }

    @Setup(Level.Iteration)
    public void setupIteration() {
        Position position = new Position(0, 0, 0, System.currentTimeMillis());
        lockBasedMongging = new Mongging(1L, position, playerInfo);
        synchronizedMongging = new MonggingSynchronized(1L, position, playerInfo);
    }

    @Benchmark
    public void testLockBasedInventoryOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(4)) {
            case 0:
                blackhole.consume(lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(lockBasedMongging.canAddItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 2:
                blackhole.consume(lockBasedMongging.countItem(ItemDictionary.TASER.boxableItem));
                break;
            case 3:
                blackhole.consume(lockBasedMongging.getItems());
                break;
        }
    }

    @Benchmark
    public void testSynchronizedInventoryOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(4)) {
            case 0:
                blackhole.consume(synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(synchronizedMongging.canAddItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 2:
                blackhole.consume(synchronizedMongging.countItem(ItemDictionary.TASER.boxableItem));
                break;
            case 3:
                blackhole.consume(synchronizedMongging.getItems());
                break;
        }
    }

    @Benchmark
    public void testLockBasedStatusOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(4)) {
            case 0:
                blackhole.consume(lockBasedMongging.getHit(10));
                break;
            case 1:
                blackhole.consume(lockBasedMongging.isNotDead());
                break;
            case 2:
                blackhole.consume(lockBasedMongging.isDead());
                break;
            case 3:
                blackhole.consume(lockBasedMongging.isKnockout());
                break;
        }
    }

    @Benchmark
    public void testSynchronizedStatusOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(4)) {
            case 0:
                blackhole.consume(synchronizedMongging.getHit(10));
                break;
            case 1:
                blackhole.consume(synchronizedMongging.isNotDead());
                break;
            case 2:
                blackhole.consume(synchronizedMongging.isDead());
                break;
            case 3:
                blackhole.consume(synchronizedMongging.isKnockout());
                break;
        }
    }

    @Benchmark
    public void testLockBasedMixedOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(6)) {
            case 0:
                blackhole.consume(lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(lockBasedMongging.getHit(5));
                break;
            case 2:
                blackhole.consume(lockBasedMongging.isNotDead());
                break;
            case 3:
                blackhole.consume(lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 4:
                blackhole.consume(lockBasedMongging.getItems());
                break;
            case 5:
                blackhole.consume(lockBasedMongging.useDefibrillator());
                break;
        }
    }

    @Benchmark
    public void testSynchronizedMixedOperations(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(6)) {
            case 0:
                blackhole.consume(synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(synchronizedMongging.getHit(5));
                break;
            case 2:
                blackhole.consume(synchronizedMongging.isNotDead());
                break;
            case 3:
                blackhole.consume(synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 4:
                blackhole.consume(synchronizedMongging.getItems());
                break;
            case 5:
                blackhole.consume(synchronizedMongging.useDefibrillator());
                break;
        }
    }

    public static void main(String[] args) throws RunnerException {
        Options options = new OptionsBuilder()
                .include(MonggingPerformanceBenchmark.class.getSimpleName())
                .jvmArgs("-Xmx2g", "-Xms2g")
                .build();

        new Runner(options).run();
    }
}