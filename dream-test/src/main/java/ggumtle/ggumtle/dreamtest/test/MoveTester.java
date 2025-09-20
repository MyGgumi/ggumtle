package ggumtle.ggumtle.dreamtest.test;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;
import java.util.Random;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MoveTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    public void run() {
        log.info("이동 테스트 시작");

        // 이동 테스트 시나리오
        int steps = 1000000;
        List<int[]> path = generateMovePaths(steps);
        int waitingMs = 16;

        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        Client[] clients = testContext.initializeClients(roomId, clientSize, packetHandler);

        // [테스트]
        // 이동
        for  (int i = 0; i < clients.length; i++) {
            int index = i;

            new Thread(() -> {
                for (int t = 0; t < path.size(); t++) {
                    clients[index].sendMoveMessage(path.get(t)[0], path.get(t)[1], path.get(t)[2]);

                    try {
                        Thread.sleep(waitingMs);
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                }
            }).start();
        }

        // 액션
        new Thread(() -> {
            Random random = new Random();

            for (int t = 0; t < path.size(); t++) {
                try {
                    Thread.sleep(2 * 1000);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }

                int index = random.nextInt(path.size());
                int command = random.nextInt(3);

                switch (command) {
                    case 0: clients[index].sendEscapeMessage(0);
                    case 1: clients[index].sendDigUpMessage(0);
                    case 2: clients[index].sendShowBoxMessage(0);
                }
            }
        }).start();
    }

    public List<int[]> generateMovePaths(int steps) {
        List<int[]> path = new ArrayList<>(steps);

        int x = 0, y = 0, z = 0;
        for (int i = 0; i < steps; i++) {
            path.add(new int[]{x, y, z});

            x += 1;
            y += 1;
            z += 1;
        }

        return path;
    }
}
