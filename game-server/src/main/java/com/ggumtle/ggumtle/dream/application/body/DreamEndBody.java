package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.player.Mongging;
import com.ggumtle.ggumtle.dream.domain.player.Player;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.Collection;
import java.util.Set;

public record DreamEndBody(
        Result result,
        Set<Long> escapedMonggings,
        Collection<Player> players
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        MONGGING_WIN((byte) 1), MONGDUNG_WIN((byte) 2);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(1 + 4 + 12 * players.size() - 1);

        buffer.put(result.value);
        buffer.putInt(players.size() - 1);
        for (Player player : players) {
            if (!(player instanceof Mongging mongging)) {
                continue;
            }
            buffer.putLong(mongging.getId());
            buffer.putInt(getStatus(mongging));
        }

        return new byte[0];
    }

    private int getStatus(Mongging mongging) {
        if (escapedMonggings.contains(mongging.getId())) {
            return 1;
        }

        if (mongging.isNotDead()) {
            return 3;
        }

        return 2;
    }
}
