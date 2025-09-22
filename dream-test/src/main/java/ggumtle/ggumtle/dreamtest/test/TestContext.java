package ggumtle.ggumtle.dreamtest.test;

import ggumtle.ggumtle.dreamtest.util.TokenParser;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.util.concurrent.ConcurrentHashMap;

@Component
@Slf4j
public class TestContext {
    private final TokenParser tokenParser;
    public final String host;
    public final int port;
    public final String[] tokens;

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
    }

    public ConcurrentHashMap<Long, Client> initializeClients(long roomId, int size, PacketHandler packetHandler) {
        ConcurrentHashMap<Long, Client> clients = new ConcurrentHashMap<>(size);
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

        return clients;
    }
}
