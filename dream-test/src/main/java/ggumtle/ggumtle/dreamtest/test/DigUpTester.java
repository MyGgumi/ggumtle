package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import ggumtle.ggumtle.dreamtest.event.PlayerInfoEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicBoolean;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class DigUpTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private boolean isRunning = false;
    private AtomicBoolean isPlayerInitialized = new AtomicBoolean(false);

    private ConcurrentHashMap<Long, Client> clients;
    private Long monggingId1 = null;
    private Long monggingId2 = null;

    public void run() {
        this.isRunning = true;

        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        this.clients = testContext.initializeClients(roomId, clientSize, packetHandler);

        try {
            Thread.sleep(5000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        testDigUp();
    }

    @EventListener
    private void handlePlayerInfo(PlayerInfoEvent event) {
        if (!isRunning) {
            return;
        }

        boolean success = this.isPlayerInitialized.compareAndSet(false, true);
        if (!success) {
            return;
        }

        log.info(event.players().toString());
        for (PlayerInfoEvent.Player player : event.players()) {
            if (!player.isMongging()) {
                continue;
            }

            if (this.monggingId1 == null) {
                this.monggingId1 = player.id();
                continue;
            }

            if (this.monggingId2 == null) {
                this.monggingId2 = player.id();
                continue;
            }
        }

        log.info("플레이어 몽깅이 두마리 설정 완료: {}, {}", monggingId1, monggingId2);
    }

    private void testDigUp() {
        log.info("꿈틀이 파기 테스트 시작");

        Client client1 = this.clients.getOrDefault(monggingId1, null);
        Client client2 = this.clients.getOrDefault(monggingId2, null);

        if (client1 == null || client2 == null) {
            log.error("클라이언트가 정상적으로 초기화 되지 않음");
            return;
        }

        log.info("0번째 사용자의 꿈틀이 파기 요청");
        client1.sendDigUpMessage(1);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("0번째 사용자의 꿈틀이 파기 중단 요청");
        client1.sendStopDiggingMessage();

        try {
            Thread.sleep(2000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("1번째 사용자의 꿈틀이 파기 요청");
        client2.sendDigUpMessage(1);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("0번째 사용자의 꿈틀이 파기 요청");
        client1.sendDigUpMessage(1);
    }
}
