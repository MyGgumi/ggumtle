package com.ggumtle.ggumtle.tick;

import com.ggumtle.ggumtle.room.domain.Room;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

import java.util.List;
import java.util.concurrent.CopyOnWriteArrayList;

@RequiredArgsConstructor
@Slf4j
public class TickWorker implements Runnable {
    private List<Room> rooms = new CopyOnWriteArrayList<>();

    private void assign(Room room) {
        rooms.add(room);
    }

    private void unassign(Room room) {
        rooms.remove(room);
    }

    @Override
    public void run() {
        while (true) {
            try {
                // TODO: 스레드 Sleep 시간을 Tick rate에 맞춰 수정
                Thread.sleep(16);
            } catch (Exception e) {
                log.error("TickWorker가 sleep 하던 중 오류 발생");
            }

            for(Room room : rooms) {
                room.tick();

                room.flush();
            }
        }
    }
}
