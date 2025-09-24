package ggumtle.ggumtle.dreamtest.test;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MoveTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private final int waitingMs = 16;
    private ConcurrentHashMap<Long, Client> clients;
    private List<Client> monggingClients;
    private Client mongdungClient;
    private long monggingId;
    private ConcurrentHashMap<Long, int[]> spawns;
    private Map<Long, List<int[]>> paths;
    private int steps = 1_000_000;

    public void run() {
        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        this.clients = testContext.initializeClients(roomId, clientSize, packetHandler);
        log.info("클라이언트 초기화 완료: {}", this.clients);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        monggingClients = testContext.monggingClients();
        mongdungClient = testContext.mongdungClient();
        spawns = testContext.spawns();
        for (Long id : clients.keySet()) {
            if (monggingClients.contains(clients.get(id))) {
                monggingId = id;
                break;
            }
        }
        generateMoveScenario();

        log.info("테스트 시나리오 설정 완료");

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("이동 테스트 시작");

        for (Map.Entry<Long, List<int[]>> entry : paths.entrySet()) {
            final long id = entry.getKey();
            final List<int[]> path = entry.getValue();

            new Thread(() -> {
                for (int step = 0; step < path.size(); step++) {
                    clients.get(id).sendMoveMessage(path.get(step)[0], path.get(step)[1], path.get(step)[2]);

                    try {
                        Thread.sleep(waitingMs);
                    } catch (InterruptedException e) {
                        e.printStackTrace();
                    }
                }
            }).start();

            new Thread(() -> {
                for (int i = 0; i < monggingClients.size(); i++) {
                    for (int j = 0; j < path.size() / 100; j++) {
                        if (j % 4 == 0 || j % 4 == 1) {
                            monggingClients.get(i).sendDigUpMessage(1);
                        }

                        if (j % 4 == 2) {
                            monggingClients.get(i).sendShowBoxMessage(1);
                        }

                        if (j % 4 == 3) {
                            mongdungClient.sendHitMonggingMessage(1, 1, 1, monggingId);
                        }

                        try {
                            Thread.sleep(waitingMs * 100);
                        } catch (InterruptedException e) {
                            e.printStackTrace();
                        }
                    }
                }
            });
        }
    }

    private void generateMoveScenario() {
        this.paths = new HashMap<>();

        for (Map.Entry<Long, int[]> spawn : this.spawns.entrySet()) {

            List<int[]> path = new ArrayList<>();

            int x = spawn.getValue()[0], y = spawn.getValue()[1], z = spawn.getValue()[2];
            for (int i = 0; i < this.steps; i++) {
                path.add(new int[]{x, y, z});
                x++;
                y++;
                z++;
            }

            this.paths.put(spawn.getKey(), path);
        }
    }
}
