package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Player;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;

public record InitializePlayerResult(
        List<Player> players,
        long receiverId
) implements Result {
    private static final int PLAYER_INFO_SIZE = 8 + 1 + 1 + 4 * 3 + 4 + 4 * 3;

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + PLAYER_INFO_SIZE * players.size());

        buffer.putInt(players.size());
        for (Player player : players) {
            buffer.putLong(player.getId());

            buffer.put((byte) (player.getId() == receiverId ? 1 : 0));
            buffer.put((byte) 1);

            buffer.putInt(player.getPositions()[player.getTail() - 1].getX());
            buffer.putInt(player.getPositions()[player.getTail() - 1].getY());
            buffer.putInt(player.getPositions()[player.getTail() - 1].getZ());

            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
            buffer.putInt(0);
        }

        return buffer.array();
    }
}
