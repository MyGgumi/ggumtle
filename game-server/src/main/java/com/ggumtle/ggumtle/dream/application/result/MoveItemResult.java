package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.vo.Item;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record MoveItemResult(
    MoveResult result,
    Item[] items
) implements Result {
    private static final int bufferSize = 4 + 4 + 4 * Box.BOX_SIZE;

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(bufferSize);

        buffer.putInt(result.value);

        if (result == MoveResult.SUCCESS) {
            buffer.putInt(Box.BOX_SIZE);
            for (int i = 0; i < Box.BOX_SIZE; i++) {
                buffer.putInt(items[i].getId());
            }

            return buffer.array();
        }

        buffer.putInt(-1);
        for (int i = 0; i <= Box.BOX_SIZE; i++) {
            buffer.putInt(-1);
        }

        return buffer.array();
    }

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum MoveResult {
        SUCCESS(1), FAIL(0),
        INDEX_OUT_OF_RANGE(2), NOT_FOUNT_DIR(3), NOT_FOUND_BOX(4), NOT_FOUND_PLAYER(5), NOT_MONGGING(6),
        NOT_FOUND_ITEM(7), FULL_ABOUT_ITEM(8)
        ;

        private final int value;
    }
}
