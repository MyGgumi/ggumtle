package com.ggumtle.ggumtle.tick;

import com.ggumtle.ggumtle.room.domain.Room;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;

import java.util.ArrayList;
import java.util.List;

@Slf4j
public class TickWorker implements Runnable {
    private static final int TICK_RATE = 30;
    private static final long TICK_INTERVAL_MS = 1000 / TICK_RATE;

    @Getter
    private final int id;
    private final List<Room> rooms;

    public TickWorker(int id) {
        this.id = id;
        this.rooms = new ArrayList<>();
    }

    public void assign(Room room) {
        rooms.add(room);
        log.info("{}번 TickWorker에 {}번 방이 할당되었습니다. 현재 관리 중인 Room 수: {}", room.id, this.id, rooms.size());
    }

    public void unassign(Room room) {
        rooms.remove(room);
        log.info("{}번 TickWorker에서 {}번 방이 제거되었습니다. 현재 관리 중인 Room 수: {}", room.id, this.id, rooms.size());
    }

    public int getRoomCount() {
        return rooms.size();
    }

    @Override
    public void run() {
        log.info("{}번 TickWorker 시작 - Tick Rate: {}, Tick Interval: {}ms", this.id, TICK_RATE, TICK_INTERVAL_MS);

        while (!Thread.currentThread().isInterrupted()) {
            long tickStartTime = System.currentTimeMillis();

            for (Room room : rooms) {
                room.tick();
                room.flush();
            }

            long tickEndTime = System.currentTimeMillis();
            long tickDuration = tickEndTime - tickStartTime;
            long sleepTime = TICK_INTERVAL_MS - tickDuration;

            if (sleepTime > 0) {
                try {
                    Thread.sleep(sleepTime);
                } catch (InterruptedException e) {
                    log.info("{}번 TickWorker가 인터럽트되어 종료됩니다.", this.id);
                    Thread.currentThread().interrupt();
                    break;
                }
            } else {
                log.warn("{}번 TickWorker에서 Tick 처리 시간({}ms)이 Tick Interval({}ms)을 초과했습니다.", this.id, tickDuration, TICK_INTERVAL_MS);
            }
        }
    }
}
