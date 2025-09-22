package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.event.PlayerInfoEvent;
import ggumtle.ggumtle.dreamtest.event.StartGameEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MoveTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private boolean isRunning = false;

    private final int waitingMs = 16;
    private ConcurrentHashMap<Long, Client> clients;
    private Map<Long, PlayerInfoEvent.Player> players;
    Map<Long, List<int[]>> paths;
    private int steps = 1_000_000;

    public void run() {
        this.isRunning = true;

        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        this.clients = testContext.initializeClients(roomId, clientSize, packetHandler);
        log.info("클라이언트 초기화 완료: {}", this.clients);
    }

    @EventListener
    private void handlePlayerInfo(PlayerInfoEvent event) {
        if (!isRunning) {
            return;
        }

        if (this.players != null) {
            return;
        }

        this.players = event.players().stream().collect(
                Collectors.toMap(
                        player -> player.id(),
                        player -> player
                )
        );

        generateMoveScenario();

        log.info("이동 시나리오 생성 완료");
    }

    @EventListener
    private void handleStartGame(StartGameEvent event) {
        if (!isRunning) {
            return;
        }

        testMove();
    }

    private void generateMoveScenario() {
        this.paths = new HashMap<>();

        for (PlayerInfoEvent.Player player : this.players.values()) {
            List<int[]> path = new ArrayList<>();

            int x = player.x(), y = player.y(), z = player.z();
            for (int i = 0; i < this.steps; i++) {
                path.add(new int[]{x, y, z});
                x++;
                y++;
                z++;
            }

            this.paths.put(player.id(), path);
        }
    }

    private void testMove() {
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
        }
    }
}
