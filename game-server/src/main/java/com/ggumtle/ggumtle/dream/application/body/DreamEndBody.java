package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.player.Mongdung;
import com.ggumtle.ggumtle.dream.domain.player.Mongging;
import com.ggumtle.ggumtle.dream.domain.player.Player;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.Collection;

public record DreamEndBody(
        boolean isMonggingWin,
        Collection<Player> players
) implements Body {

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(1 + 4 + 4 + 16 * players.size());

        // 게임 결과
        buffer.put(isMonggingWin ? (byte) 1 : (byte) 2);

        // 탈출한 몽깅이 수
        long escapedMonggingCount = this.players.stream()
                .filter(player -> player instanceof Mongging mongging && mongging.isEscaped())
                .count();
        buffer.putInt((int) escapedMonggingCount);

        // 플레이어들 상태
        buffer.putInt(this.players.size());
        for (Player player : this.players) {
            buffer.putLong(player.getId());

            if (player instanceof Mongging mongging) {
                buffer.putInt(mongging.isEscaped() ? 1 : 2);
                buffer.putInt(isMonggingWin ? 100 : 0);
            }

            else if (player instanceof Mongdung) {
                buffer.putInt(1);
                buffer.putInt(isMonggingWin ? 0 : 150);
            }
        }

        return buffer.array();
    }
}
