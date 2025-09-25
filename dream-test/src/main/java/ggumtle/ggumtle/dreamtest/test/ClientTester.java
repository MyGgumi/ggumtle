package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class ClientTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private ConcurrentHashMap<Long, Client> clients;
    private List<Client> monggingClients;
    private Client mongdungClient;

    public void run() {
        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        this.clients = testContext.initializeClients(roomId, clientSize, packetHandler);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        monggingClients = testContext.monggingClients();
        mongdungClient = testContext.mongdungClient();

        log.info("테스트 시나리오 설정 완료");

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("테스트 시작");

        // 테스트 할 로직 작성
    }
}
