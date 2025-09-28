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
@Warmup(iterations = 3, time = 2, timeUnit = TimeUnit.SECONDS)
@Measurement(iterations = 5, time = 3, timeUnit = TimeUnit.SECONDS)
@Fork(1)
public class ComprehensiveMonggingBenchmark {

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

    // ========================================
    // 1. 기본 인벤토리 작업 테스트 (Original MonggingPerformanceBenchmark)
    // ========================================

    @Benchmark
    public void lockBasedInventoryOperations(Blackhole blackhole) {
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
    public void synchronizedInventoryOperations(Blackhole blackhole) {
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

    // ========================================
    // 2. 상태 작업 테스트 (getHit 제외)
    // ========================================

    @Benchmark
    public void lockBasedStatusOperationsWithoutGetHit(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(3)) {
            case 0:
                blackhole.consume(lockBasedMongging.isNotDead());
                break;
            case 1:
                blackhole.consume(lockBasedMongging.isDead());
                break;
            case 2:
                blackhole.consume(lockBasedMongging.isKnockout());
                break;
        }
    }

    @Benchmark
    public void synchronizedStatusOperationsWithoutGetHit(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(3)) {
            case 0:
                blackhole.consume(synchronizedMongging.isNotDead());
                break;
            case 1:
                blackhole.consume(synchronizedMongging.isDead());
                break;
            case 2:
                blackhole.consume(synchronizedMongging.isKnockout());
                break;
        }
    }

    // ========================================
    // 3. 상태 작업 테스트 (getHit 포함) - 이중 락 영향 측정
    // ========================================

    @Benchmark
    public void lockBasedStatusOperationsWithGetHit(Blackhole blackhole) {
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
    public void synchronizedStatusOperationsWithGetHit(Blackhole blackhole) {
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

    // ========================================
    // 4. 혼합 작업 테스트 (getHit 제외)
    // ========================================

    @Benchmark
    public void lockBasedMixedOperationsWithoutGetHit(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(5)) {
            case 0:
                blackhole.consume(lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(lockBasedMongging.isNotDead());
                break;
            case 2:
                blackhole.consume(lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 3:
                blackhole.consume(lockBasedMongging.getItems());
                break;
            case 4:
                blackhole.consume(lockBasedMongging.useDefibrillator());
                break;
        }
    }

    @Benchmark
    public void synchronizedMixedOperationsWithoutGetHit(Blackhole blackhole) {
        ThreadLocalRandom random = ThreadLocalRandom.current();

        switch (random.nextInt(5)) {
            case 0:
                blackhole.consume(synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
                break;
            case 1:
                blackhole.consume(synchronizedMongging.isNotDead());
                break;
            case 2:
                blackhole.consume(synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem));
                break;
            case 3:
                blackhole.consume(synchronizedMongging.getItems());
                break;
            case 4:
                blackhole.consume(synchronizedMongging.useDefibrillator());
                break;
        }
    }

    // ========================================
    // 5. 혼합 작업 테스트 (getHit 포함)
    // ========================================

    @Benchmark
    public void lockBasedMixedOperationsWithGetHit(Blackhole blackhole) {
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
    public void synchronizedMixedOperationsWithGetHit(Blackhole blackhole) {
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

    // ========================================
    // 6. 그룹 테스트 - 동시 접근 시나리오 (getHit 제외)
    // ========================================

    @Benchmark
    @Group("lockBasedConcurrentWithoutGetHit")
    @GroupThreads(4)
    public void lockBasedInventoryWorkWithoutGetHit(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(lockBasedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(lockBasedMongging.getItems());
        blackhole.consume(lockBasedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("lockBasedConcurrentWithoutGetHit")
    @GroupThreads(4)
    public void lockBasedStatusWorkWithoutGetHit(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.isNotDead());
        blackhole.consume(lockBasedMongging.isDead());
        blackhole.consume(lockBasedMongging.isKnockout());
        blackhole.consume(lockBasedMongging.isEscaped());
    }

    @Benchmark
    @Group("synchronizedConcurrentWithoutGetHit")
    @GroupThreads(4)
    public void synchronizedInventoryWorkWithoutGetHit(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(synchronizedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(synchronizedMongging.getItems());
        blackhole.consume(synchronizedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("synchronizedConcurrentWithoutGetHit")
    @GroupThreads(4)
    public void synchronizedStatusWorkWithoutGetHit(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.isNotDead());
        blackhole.consume(synchronizedMongging.isDead());
        blackhole.consume(synchronizedMongging.isKnockout());
        blackhole.consume(synchronizedMongging.isEscaped());
    }

    // ========================================
    // 7. 그룹 테스트 - 동시 접근 시나리오 (getHit 포함)
    // ========================================

    @Benchmark
    @Group("lockBasedConcurrentWithGetHit")
    @GroupThreads(4)
    public void lockBasedInventoryWorkWithGetHit(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(lockBasedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(lockBasedMongging.getItems());
        blackhole.consume(lockBasedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("lockBasedConcurrentWithGetHit")
    @GroupThreads(4)
    public void lockBasedStatusWorkWithGetHit(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.getHit(5));
        blackhole.consume(lockBasedMongging.isNotDead());
        blackhole.consume(lockBasedMongging.isDead());
        blackhole.consume(lockBasedMongging.isKnockout());
    }

    @Benchmark
    @Group("synchronizedConcurrentWithGetHit")
    @GroupThreads(4)
    public void synchronizedInventoryWorkWithGetHit(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.addItem(ItemDictionary.FLASH.boxableItem));
        blackhole.consume(synchronizedMongging.countItem(ItemDictionary.LIGHT_JELLY.boxableItem));
        blackhole.consume(synchronizedMongging.getItems());
        blackhole.consume(synchronizedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    @Group("synchronizedConcurrentWithGetHit")
    @GroupThreads(4)
    public void synchronizedStatusWorkWithGetHit(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.getHit(5));
        blackhole.consume(synchronizedMongging.isNotDead());
        blackhole.consume(synchronizedMongging.isDead());
        blackhole.consume(synchronizedMongging.isKnockout());
    }

    // ========================================
    // 8. getHit 이중 락 영향 측정 전용 테스트
    // ========================================

    @Benchmark
    public void lockBasedGetHitOnlyTest(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.getHit(ThreadLocalRandom.current().nextInt(1, 20)));
    }

    @Benchmark
    public void synchronizedGetHitOnlyTest(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.getHit(ThreadLocalRandom.current().nextInt(1, 20)));
    }

    // ========================================
    // 9. 순수 단일 기능 테스트 - JVM 최적화 차이만 측정
    // ========================================

    @Benchmark
    public void pureInventoryOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
    }

    @Benchmark
    public void pureInventoryOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem));
    }

    @Benchmark
    public void pureStatusOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.isNotDead());
    }

    @Benchmark
    public void pureStatusOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.isNotDead());
    }

    @Benchmark
    public void pureCountItemOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem));
    }

    @Benchmark
    public void pureCountItemOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem));
    }

    @Benchmark
    public void pureGetItemsOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.getItems());
    }

    @Benchmark
    public void pureGetItemsOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.getItems());
    }

    @Benchmark
    public void pureCanAddItemOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    public void pureCanAddItemOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.canAddItem(ItemDictionary.TASER.boxableItem));
    }

    @Benchmark
    public void pureGetHitOnlyLockBased(Blackhole blackhole) {
        blackhole.consume(lockBasedMongging.getHit(5));
    }

    @Benchmark
    public void pureGetHitOnlySynchronized(Blackhole blackhole) {
        blackhole.consume(synchronizedMongging.getHit(5));
    }

    // ========================================
    // 10. 멀티스레드 실제 시나리오 테스트
    // ========================================

    @Benchmark
    public void lockBasedMultiThreadScenarioWithoutGetHit(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                lockBasedMongging.isNotDead();
                lockBasedMongging.isDead();
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
    public void synchronizedMultiThreadScenarioWithoutGetHit(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                synchronizedMongging.isNotDead();
                synchronizedMongging.isDead();
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

    @Benchmark
    public void lockBasedMultiThreadScenarioWithGetHit(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                lockBasedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                lockBasedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                lockBasedMongging.getHit(1);
                lockBasedMongging.isNotDead();
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
    public void synchronizedMultiThreadScenarioWithGetHit(Blackhole blackhole) {
        Thread inventoryThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                synchronizedMongging.addItem(ItemDictionary.LIGHT_JELLY.boxableItem);
                synchronizedMongging.countItem(ItemDictionary.FLASH.boxableItem);
            }
        });

        Thread statusThread = new Thread(() -> {
            for (int i = 0; i < 50; i++) {
                synchronizedMongging.getHit(1);
                synchronizedMongging.isNotDead();
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
                .include(ComprehensiveMonggingBenchmark.class.getSimpleName())
                .jvmArgs("-Xmx2g", "-Xms2g")
                .result("comprehensive_benchmark_results.txt")
                .build();

        new Runner(options).run();
    }
}