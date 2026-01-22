package com.ggumtle.ggumtle.tick;

import com.ggumtle.ggumtle.room.domain.Room;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

@Component
@Slf4j
public class TickThreadPool {
    // TODO: TickWorker가 처리량 확인 후 수정
    private static final int WORKER_COUNT = 1;
    private final List<TickWorker> tickWorkers;
    private final Map<Room, TickWorker> roomWorkerMap;
    private final ExecutorService executor;

    private int pointer = 0;

    public TickThreadPool() {
        executor = Executors.newFixedThreadPool(WORKER_COUNT);

        tickWorkers = new ArrayList<>(WORKER_COUNT);
        for (int i = 0; i < WORKER_COUNT; i++) {
            tickWorkers.add(new TickWorker(i));
        }

        roomWorkerMap = new HashMap<>();
    }

    @PostConstruct
    public void start() {
        log.info("TickThreadPool 시작");
    }

    @PreDestroy
    public void shutdown() {
        log.info("TickThreadPool 종료 중...");

        executor.shutdown();
        try {
            if (!executor.awaitTermination(5, TimeUnit.SECONDS)) {
                executor.shutdownNow();
            }
        } catch (InterruptedException e) {
            executor.shutdownNow();
            Thread.currentThread().interrupt();
        }

        log.info("TickThreadPool 종료 완료");
    }

    public void assignRoom(Room room) {
        TickWorker tickWorker = tickWorkers.get(pointer++);
        tickWorker.assign(room);
        roomWorkerMap.put(room, tickWorker);
        log.error("{}번 Room의 {}번 TickWorker에 할당", room.id, tickWorker.getId());
    }

    public void unassignRoom(Room room) {
        TickWorker tickWorker = roomWorkerMap.getOrDefault(room, null);
        if (tickWorker == null) {
            log.error("{}번 Room을 관리하던 TickWorker를 찾을 수 없습니다", room.id);
            return;
        }
        tickWorker.unassign(room);
        log.error("{}번 Room을 {}번 TickWorker에서 할당 해제 완료", room.id, tickWorker.getId());
    }
}
