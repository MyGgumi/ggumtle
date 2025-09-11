package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Mongdung;
import com.ggumtle.ggumtle.dream.domain.Mongging;
import com.ggumtle.ggumtle.dream.domain.Player;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;

public record InitializePlayerResult(
        List<Player> players,
        long receiverId
) implements Result {
    private static final int PLAYER_INFO_SIZE = 8 + 1 + 1 + 4 * 3 + 4 + 4 * 3;

    // TODO: HP로 수정
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + PLAYER_INFO_SIZE * players.size());

        buffer.putInt(players.size());
        for (Player player : players) {
            buffer.putLong(player.getId());

            buffer.put((byte) (player.getId() == receiverId ? 1 : 0));
            buffer.put((byte) 1);

            buffer.putInt(player.getPositions()[player.getCurr()].x);
            buffer.putInt(player.getPositions()[player.getCurr()].y);
            buffer.putInt(player.getPositions()[player.getCurr()].z);

            if (player instanceof Mongging mongging) {
                buffer.putInt(mongging.maxHp);
                buffer.putInt(mongging.moveSpeed);
                buffer.putInt(mongging.healSpeed);
                buffer.putInt(mongging.workSpeed);
            }

            if (player instanceof Mongdung mongdung) {
                buffer.putInt(-1);
                buffer.putInt(mongdung.moveSpeed);
                buffer.putInt(-1);
                buffer.putInt(-1);

            }
        }

        return buffer.array();
    }
}
