package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.event.TakeItemEvent;
import ggumtle.ggumtle.dreamtest.event.PutItemEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import ggumtle.ggumtle.dreamtest.config.PacketHandler;

import java.util.HashMap;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MyChestTester {
    private final TestContext testContext;
    private final PacketHandler packetHandler;

    private ConcurrentHashMap<Long, Client> clients;
    private Client mongdungClient;
    private List<Client> monggingClients;
    private ConcurrentHashMap<Client, HashMap<Integer, Integer>> clientInventories;

    private boolean isRunner = false;

    public void run() {
        isRunner = true;

        long roomId = -1L;
        int clientSize = 3;
        clients = testContext.initializeClients(roomId, clientSize, packetHandler);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        monggingClients = testContext.monggingClients();
        mongdungClient = testContext.mongdungClient();

        if (monggingClients == null || mongdungClient == null) {
            log.error("클라이언트 초기화 실패");
            return;
        }

        clientInventories = new ConcurrentHashMap<>();

        for (Client client : clients.values()) {
            clientInventories.put(client, new HashMap<>());
        }

        log.info("테스트 시나리오 설정 완료");

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        testMongdungOpenChest(mongdungClient);

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        test_동시에_상자_열어서_아이템_가져가기(monggingClients.get(0), monggingClients.get(1));

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        test_같은_상자에서_아이템_주고받기_반복(monggingClients.get(0), monggingClients.get(1));
    }

    private void testMongdungOpenChest(Client mongdungClient) {
        log.info("몽등이 상자 열기 테스트 시작");

        mongdungClient.sendShowBoxMessage(1);

        log.info("몽등이 상자 열기 테스트 완료");
    }

    private void test_동시에_상자_열어서_아이템_가져가기(Client mongging1, Client mongging2) {
        log.info("몽깅이 상자 동시에 열어서 아이템 가져가기 테스트 시작");

        mongging1.sendShowBoxMessage(1);
        mongging2.sendShowBoxMessage(1);

        log.info("상자에 아이템을 하나 가져감");
        mongging1.sendTakeItemMessage(1, 0);

        log.info("가져간 곳을 가져가려고 함");
        mongging2.sendTakeItemMessage(1, 0);

        log.info("몽깅이1 상자 닫음");
        mongging1.sendCloseBoxMessage(1);

        log.info("몽깅이2 아이템 가져가기");
        mongging2.sendTakeItemMessage(1, 1);

        log.info("몽깅이2 상자 닫음");
        mongging2.sendCloseBoxMessage(1);

        log.info("몽깅이 상자 동시에 열어서 아이템 가져가기 테스트 완료");
    }

    private void test_같은_상자에서_아이템_주고받기_반복(Client mongging1, Client mongging2) {
        log.info("같은 상자에서 아이템 주고받기 1000번 반복 테스트 시작");
        
        int boxId = 2;
        int repeatCount = 100;
        
        
        // 두 클라이언트 모두 상자 열기
        mongging1.sendShowBoxMessage(boxId);
        mongging2.sendShowBoxMessage(boxId);
        
        try {
            Thread.sleep(500); // 상자 열기 대기
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
        
        for (int i = 0; i < repeatCount; i++) {
            try {
                // mongging1이 첫 번째 슬롯(index 0)에서 아이템 가져가기
                mongging1.sendTakeItemMessage(boxId, 0);
                Thread.sleep(50); // 아이템 가져가기 처리 대기
                
                // mongging2가 두 번째 슬롯(index 1)에서 아이템 가져가기  
                mongging2.sendTakeItemMessage(boxId, 1);
                Thread.sleep(50); // 아이템 가져가기 처리 대기
                
                // 인벤토리에서 아이템 확인하여 다시 넣기
                HashMap<Integer, Integer> mongging1Inventory = clientInventories.get(mongging1);
                HashMap<Integer, Integer> mongging2Inventory = clientInventories.get(mongging2);
                
                // mongging1 인벤토리에서 아이템이 있으면 하나 넣기
                for (Integer itemId : mongging1Inventory.keySet()) {
                    if (mongging1Inventory.get(itemId) > 0) {
                        mongging1.sendPutItemMessage(boxId, itemId);
                        // 인벤토리에서 아이템 하나 제거
                        mongging1Inventory.put(itemId, mongging1Inventory.get(itemId) - 1);
                        if (mongging1Inventory.get(itemId) == 0) {
                            mongging1Inventory.remove(itemId);
                        }
                        Thread.sleep(50);
                        break; // 하나만 넣기
                    }
                }
                
                // mongging2 인벤토리에서 아이템이 있으면 하나 넣기
                for (Integer itemId : mongging2Inventory.keySet()) {
                    if (mongging2Inventory.get(itemId) > 0) {
                        mongging2.sendPutItemMessage(boxId, itemId);
                        // 인벤토리에서 아이템 하나 제거
                        mongging2Inventory.put(itemId, mongging2Inventory.get(itemId) - 1);
                        if (mongging2Inventory.get(itemId) == 0) {
                            mongging2Inventory.remove(itemId);
                        }
                        Thread.sleep(50);
                        break; // 하나만 넣기
                    }
                }
                
                if ((i + 1) % 100 == 0) {
                    log.info("반복 진행 상황: {}/{}", i + 1, repeatCount);
                }
                
            } catch (InterruptedException e) {
                log.error("테스트 중 인터럽트 발생", e);
                break;
            }
        }
        
        // 테스트 완료 후 상자 닫기
        mongging1.sendCloseBoxMessage(boxId);
        mongging2.sendCloseBoxMessage(boxId);
        
        log.info("같은 상자에서 아이템 주고받기 1000번 반복 테스트 완료");
    }

    @EventListener
    public void onTakeItemEvent(TakeItemEvent event) {
        if (!isRunner) {
            return;
        }

        if (event.success()) {
            log.info("아이템 가져가기 성공: playerId={}, boxId={}, takenItem={}", event.playerId(), event.boxId(), event.takenItem());


            HashMap<Integer, Integer> inventory = clientInventories.get(clients.get(event.playerId()));
            log.info("플레이어 인벤토리: {}", inventory);
            inventory.put(event.takenItem(), inventory.getOrDefault(event.takenItem(), 0) + 1);
            log.info("플레이어 인벤토리 업데이트 후: {}", inventory);
        } else {
            log.info("아이템 가져가기 실패: playerId={}, boxId={}, takenItem={}", event.playerId(), event.boxId(), event.takenItem());
        }
    }

    @EventListener
    public void onPutItemEvent(PutItemEvent event) {
        if (!isRunner) {
            return;
        }

        if (event.result() == PutItemEvent.Result.SUCCESS) {
            log.info("아이템 넣기 성공: 상자 아이템 상태={}", java.util.Arrays.toString(event.items()));
        } else {
            log.info("아이템 넣기 실패: result={}, 상자 아이템 상태={}", event.result(), java.util.Arrays.toString(event.items()));
        }
    }
}
