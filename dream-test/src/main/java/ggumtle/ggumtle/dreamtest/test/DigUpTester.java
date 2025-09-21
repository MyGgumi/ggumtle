package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class DigUpTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    public void run() {
        log.info("꿈틀이 파기 테스트 시작");

        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        Client[] clients = testContext.initializeClients(roomId, clientSize, packetHandler);


        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        // [테스트]
        log.info("0번째 사용자의 꿈틀이 파기 요청");
        clients[0].sendDigUpMessage(1);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("0번째 사용자의 꿈틀이 파기 중단 요청");
        clients[0].sendStopDiggingMessage();

        try {
            Thread.sleep(500);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("1번째 사용자의 꿈틀이 파기 요청");
        clients[1].sendDigUpMessage(1);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        log.info("0번째 사용자의 꿈틀이 파기 요청");
        clients[0].sendDigUpMessage(1);
    }
}
