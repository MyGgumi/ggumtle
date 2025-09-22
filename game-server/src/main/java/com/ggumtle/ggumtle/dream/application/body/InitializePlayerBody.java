package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.player.Mongdung;
import com.ggumtle.ggumtle.dream.domain.player.Mongging;
import com.ggumtle.ggumtle.dream.domain.player.Player;
import com.ggumtle.ggumtle.room.domain.PlayerInfo;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;
import java.util.List;
import java.util.Map;

public record InitializePlayerBody(
        long receiverId,
        List<Player> players,
        Map<Long, PlayerInfo> playerInfos
) implements Body {
    private static final int PLAYER_INFO_SIZE = 8 + 1 + 1 + 8 + 12 + 16 + 4;

    // TODO: HP로 수정
    @Override
    public byte[] toBytes(Charset charsets) {
        int nicknameSize = playerInfos.values().stream()
                .mapToInt(p -> p.nickname.getBytes(charsets).length)
                .sum();

        ByteBuffer buffer = ByteBuffer.allocate(4 + PLAYER_INFO_SIZE * players.size() + nicknameSize);

        buffer.putInt(players.size());
        for (Player player : players) {
            PlayerInfo playerInfo = playerInfos.get(player.getId());

            buffer.putLong(player.getId());

            // 메타 정보
            buffer.put((byte) (player.getId() == receiverId ? 1 : 0));
            buffer.put(player instanceof Mongging ? (byte) 1 : (byte) 0);
            buffer.putLong(playerInfo.monggingClassId);

            // 좌표
            buffer.putInt(player.getPositions()[player.getCurr()].x);
            buffer.putInt(player.getPositions()[player.getCurr()].y);
            buffer.putInt(player.getPositions()[player.getCurr()].z);

            // 스탯
            if (player instanceof Mongging mongging) {
                buffer.putInt(mongging.maxHp);
                buffer.putInt(mongging.moveSpeed);
                buffer.putInt(mongging.healSpeed);
                buffer.putInt(mongging.workSpeed);
            }
            else if (player instanceof Mongdung mongdung) {
                buffer.putInt(-1);
                buffer.putInt(mongdung.moveSpeed);
                buffer.putInt(-1);
                buffer.putInt(-1);
            }

            // 닉네임
            byte[] nicknameBytes = playerInfo.nickname.getBytes(charsets);
            buffer.putInt(nicknameBytes.length);
            buffer.put(nicknameBytes);
        }

        return buffer.array();
    }
}
