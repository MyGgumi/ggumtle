package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class DigUpTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private List<Client> monggingClients;

    public void run() {
        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        testContext.initializeClients(roomId, clientSize, packetHandler);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        monggingClients = testContext.monggingClients();

        log.info("테스트 시나리오 설정 완료");

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        testDigUp(monggingClients.get(0), monggingClients.get(1));
    }

    private void testDigUp(Client client1, Client client2) {
        log.info("꿈틀이 파기 테스트 시작");

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
