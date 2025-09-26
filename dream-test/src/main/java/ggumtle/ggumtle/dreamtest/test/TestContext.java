package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.event.PlayerInfoEvent;
import ggumtle.ggumtle.dreamtest.util.TokenParser;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicBoolean;

@Component
@Slf4j
public class TestContext {
    public final String host;
    public final int port;

    public final TokenParser tokenParser;
    public final String[] tokens;

    private ConcurrentHashMap<Long, Client> clients;
    private AtomicBoolean initPlayer;
    private List<Client> monggings;
    private Client mongdung;
    private ConcurrentHashMap<Long, int[]> spawns;

    @Autowired
    public TestContext(
            TokenParser tokenParser,
            @Value("${HOST}") String host,
            @Value("${PORT}") int port,
            @Value("${TOKENS}") String tokens) {
        this.tokenParser = tokenParser;
        this.host = host;
        this.port = port;
        this.tokens = tokens.split(",");

        log.info("Test 환경: Host={}, port={}, toekns={}", host, port, this.tokens);

        initPlayer = new AtomicBoolean(false);
    }

    public ConcurrentHashMap<Long, Client> initializeClients(long roomId, int size, PacketHandler packetHandler) {
        clients = new ConcurrentHashMap<>(size);
        for (int i = 0; i < size; i++) {
            long id = tokenParser.parseId(tokens[i]);
            Client client = new Client(host, port, packetHandler);
            client.sendAuthMessage(tokens[i]);

            clients.put(id, client);
        }

        try {
            Thread.sleep(500);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        for (Client value : clients.values()) {
            value.sendJoinRoomMessage(roomId);
        }

        try {
            Thread.sleep(500);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        for (Client client : clients.values()) {
            client.sendSceneChangeMessage();
        }

        log.info("클라이언트 초기화 완료");

        return clients;
    }

    @EventListener
    private void initializePlayerInfo(PlayerInfoEvent event) {
        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        boolean success = initPlayer.compareAndSet(false, true);
        if (!success) {
            log.info("플레이어 정보를 이미 초기화 함");
            return;
        }

        monggings = new ArrayList<>();
        spawns = new ConcurrentHashMap<>();
        for (PlayerInfoEvent.Player player : event.players()) {
            if (player.isMongging()) {
                monggings.add(clients.get(player.id()));
            } else {
                mongdung = clients.get(player.id());
            }

            spawns.put(player.id(), new int[]{ player.x(), player.y(), player.z() });
        }
        monggings = List.copyOf(monggings);

        log.info("플레이어 정보 초기화 완료");
    }

    public List<Client> monggingClients() {
        return monggings;
    }

    public Client mongdungClient() {
        return mongdung;
    }

    public ConcurrentHashMap<Long, int[]> spawns() {
        return spawns;
    }
}
