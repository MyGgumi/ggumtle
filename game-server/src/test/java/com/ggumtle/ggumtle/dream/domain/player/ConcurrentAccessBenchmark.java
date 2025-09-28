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

import java.util.concurrent.TimeUnit;

@BenchmarkMode(Mode.Throughput)
@OutputTimeUnit(TimeUnit.SECONDS)
@State(Scope.Benchmark)
@Threads(8)
@Warmup(iterations = 3, time = 2, timeUnit = TimeUnit.SECONDS)
@Measurement(iterations = 5, time = 3, timeUnit = TimeUnit.SECONDS)
@Fork(1)
public class ConcurrentAccessBenchmark {

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

        lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
        lockBasedMongging.addItem(ItemDictionary.DEFIBRILLATOR.boxableItem);

        synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
        synchronizedMongging.addItem(ItemDictionary.DEFIBRILLATOR.boxableItem);
    }

    @Benchmark
    @Group("lockBased")
    @GroupThreads(4)
    public void lockBasedInventoryWork(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(lockBasedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(lockBasedMongging.getItems());
        blackhole.consume(lockBasedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("lockBased")
    @GroupThreads(4)
    public void lockBasedStatusWork(Blackhole blackhole) {
//        blackhole.consume(lockBasedMongging.getHit(5));
        blackhole.consume(lockBasedMongging.isNotDead());
        blackhole.consume(lockBasedMongging.isDead());
        blackhole.consume(lockBasedMongging.isKnockout());
    }

    @Benchmark
    @Group("synchronized")
    @GroupThreads(4)
    public void synchronizedInventoryWork(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(synchronizedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(synchronizedMongging.getItems());
        blackhole.consume(synchronizedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("synchronized")
    @GroupThreads(4)
    public void synchronizedStatusWork(Blackhole blackhole) {
//        blackhole.consume(synchronizedMongging.getHit(5));
        blackhole.consume(synchronizedMongging.isNotDead());
        blackhole.consume(synchronizedMongging.isDead());
        blackhole.consume(synchronizedMongging.isKnockout());
    }

    @Benchmark
    public void demonstrateLockBasedConcurrency(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 100; i++) {
                lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 100; i++) {
                lockBasedMongging.isNotDead();
                lockBasedMongging.getHit(1);
            }
        });

        inventoryThread.start();
        statusThread.start();

        try {
            inventoryThread.join();
            statusThread.join();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }

        blackhole.consume(lockBasedMongging.isNotDead());
    }

    @Benchmark
    public void demonstrateSynchronizedBlocking(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 100; i++) {
                synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 100; i++) {
                synchronizedMongging.isNotDead();
                synchronizedMongging.getHit(1);
            }
        });

        inventoryThread.start();
        statusThread.start();

        try {
            inventoryThread.join();
            statusThread.join();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
        }

        blackhole.consume(synchronizedMongging.isNotDead());
    }

    public static void main(String[] args) throws RunnerException {
        Options options = new OptionsBuilder()
                .include(ConcurrentAccessBenchmark.class.getSimpleName())
                .jvmArgs("-Xmx2g", "-Xms2g")
                .result("concurrent_access_benchmark_results.txt")
                .build();

        new Runner(options).run();
    }
}