package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.Map;

public record DreamEndBody(
        Result result,
        Map<Long, PlayerStatus> playerStatuses
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        MONGGING_WIN((byte) 1), MONGDUNG_WIN((byte) 2);

        private final byte value;
    }

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum PlayerStatus {
        ALIVE(1),  DEAD(2), ESCAPED(3);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(1 + 4 + 12 * playerStatuses.size());

        buffer.put(result.value);
        buffer.putInt(playerStatuses.size() - 1);
        for (Map.Entry<Long, PlayerStatus> player : playerStatuses.entrySet()) {
            buffer.putLong(player.getKey());
            buffer.putInt(player.getValue().value);
        }

        return buffer.array();
    }
}
