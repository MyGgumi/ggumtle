package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.domain.Box;
import com.ggumtle.ggumtle.dream.domain.Item;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record TakeItemBody(
    Result result,
    Item[] items
) implements Body {
    private static final int bufferSize = 4 + 4 + 4 * Box.BOX_SIZE;

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0),
        INDEX_OUT_OF_RANGE(2), NOT_FOUND_BOX(3), NOT_FOUND_PLAYER(4), NOT_MONGGING(5),
        NOT_FOUND_ITEM(10), FULL_ABOUT_ITEM(11)
        ;

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(bufferSize);

        buffer.putInt(result.value);

        if (result == Result.SUCCESS) {
            buffer.putInt(Box.BOX_SIZE);
            for (int i = 0; i < Box.BOX_SIZE; i++) {
                buffer.putInt(items[i] == null ? -1 : items[i].id);
            }

            return buffer.array();
        }

        buffer.putInt(-1);
        for (int i = 0; i < Box.BOX_SIZE; i++) {
            buffer.putInt(-1);
        }

        return buffer.array();
    }
}
