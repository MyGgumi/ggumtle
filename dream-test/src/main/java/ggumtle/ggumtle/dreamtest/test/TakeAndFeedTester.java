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
public class TakeAndFeedTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private List<Client> monggingClients;

    public void run() {
        // 클라이언트 생성
        long roomId = -1L;
        int clientSize = 3;
        testContext.initializeClients(roomId, clientSize, packetHandler);

        sleep(1000);

        monggingClients = testContext.monggingClients();

        log.info("테스트 시나리오 설정 완료");

        sleep(1000);

        log.info("꿈틀이 먹이기 테스트 시작");

        log.info("상자 열기");
        monggingClients.getFirst().sendShowBoxMessage(1);

        sleep(1000);

//        log.info("꿈틀이 파내기 전에 먹이기 시도");
//        monggingClients.getFirst().sendStartFeedMessage(1);
//
//        sleep(1000);

        log.info("꿈틀이 파내기");
        monggingClients.getFirst().sendDigUpMessage(1);

        sleep(3500);

//        log.info("꿈젤리 얻기 전에 먹이기 시도");
//        monggingClients.getFirst().sendStartFeedMessage(1);
//
//        sleep(1000);
//
//        log.info("1번 상자의 0번째 아이템 꺼내기 시도");
//        monggingClients.getFirst().sendTakeItemMessage(1, 0);
//
//        sleep(1000);

        log.info("꿈틀이 먹이기 시도");
        monggingClients.getFirst().sendStartFeedMessage(1);

        sleep(1000);

        log.info("꿈틀이 먹이기 중단");
        monggingClients.getFirst().sendStopFeedMessage();
    }

    private void sleep(long millis) {
        try {
            Thread.sleep(millis);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
    }
}
