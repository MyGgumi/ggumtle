package ggumtle.ggumtle.dreamtest.event;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

public record PlayerInfoEvent(
        List<Player> players
) {
    public static PlayerInfoEvent of(byte[] data) {
        ByteBuffer buffer = ByteBuffer.wrap(data);

        List<Player> players = new ArrayList<>();
        int count = buffer.getInt();
        for (int i = 0; i < count; i++) {
            long id = buffer.getLong();
            boolean isMine = buffer.get() == 1;
            boolean isMongging = buffer.get() == 1;

            int x = buffer.getInt();
            int y = buffer.getInt();
            int z = buffer.getInt();

            int moveSpeed = buffer.getInt();
            int maxHp = buffer.getInt();
            int healSpeed = buffer.getInt();
            int workSpeed = buffer.getInt();

            Player player = new Player(id, isMongging, x, y, z);
            players.add(player);
        }

        return new PlayerInfoEvent(players);
    }

    public record Player(
            long id,
            boolean isMongging,
            int x,
            int y,
            int z
    ) {
    }
}
