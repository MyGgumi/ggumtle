package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.item.Box;
import com.ggumtle.ggumtle.dream.domain.item.Boxable;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record TakeItemBody(
    Result result,
    long playerId,
    int boxId,
    Boxable[] itemsOfBox,
    Boxable takenItem
) implements Body {
    private static final int bufferSize = 4 + 8 + 4 + 4 + 4 * Box.BOX_SIZE + 4;

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0),
        INDEX_OUT_OF_RANGE(2), NOT_FOUND_BOX(3), NOT_FOUND_PLAYER(4), NOT_MONGGING(5),
        NOT_FOUND_ITEM(10), FULL_ABOUT_ITEM(11), NOT_NEAR(12)
        ;

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(bufferSize);

        // 요청 처리 결과
        buffer.putInt(this.result.value);

        // 요청한 플레이어 정보
        buffer.putLong(this.playerId);

        //.상자 정보
        buffer.putInt(this.boxId);
        buffer.putInt(Box.BOX_SIZE);
        if (result == Result.SUCCESS) {
            for (int i = 0; i < Box.BOX_SIZE; i++) {
                buffer.putInt(itemsOfBox[i] == null ? -1 : itemsOfBox[i].id);
            }
            buffer.putInt(this.takenItem.id);

            return buffer.array();
        }

        for (int i = 0; i < Box.BOX_SIZE; i++) {
            buffer.putInt(-1);
        }
        buffer.putInt(-1);

        return buffer.array();
    }
}
