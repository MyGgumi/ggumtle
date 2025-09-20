package ggumtle.ggumtle.dreamtest.test;

import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.config.PacketHandler;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

@Component
@Slf4j
public class TestContext {
    public final String host;
    public final int port;
    public final String[] accessTokens;

    public TestContext(
            @Value("${HOST}") String host,
            @Value("${PORT}") int port,
            @Value("${TOKENS}") String tokens) {
        this.host = host;
        this.port = port;
        this.accessTokens = tokens.split(",");

        log.info("Test 환경: Host={}, port={}, toekns={}", host, port, accessTokens);
    }

    public Client[] initializeClients(long roomId, int size, PacketHandler packetHandler) {
        Client[] clients = new Client[size];
        for (int i = 0; i < clients.length; i++) {
            clients[i] = new Client(this.host, this.port, packetHandler);
            clients[i].sendAuthMessage(this.accessTokens[i]);
        }

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        for (Client value : clients) {
            value.sendJoinRoomMessage(roomId);
        }

        try {
            Thread.sleep(1000);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        for (Client client : clients) {
            client.sendSceneChangeMessage();
        }

        return clients;
    }
}
