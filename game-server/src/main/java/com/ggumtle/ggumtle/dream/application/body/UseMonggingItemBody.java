package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record UseMonggingItemBody(
        Result result,
        int itemId
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        FAIL((byte) 0), SUCCESS((byte) 1),
        NOT_FOUND_ITEM_ID((byte) 2), NOT_FOUND_MONGGING((byte) 3), NOT_FOUND_MONGDUNG((byte) 4),
        NOT_ATTACK_ITEM((byte) 10), NOT_FOUND_ITEM((byte) 11), MISS((byte) 12);
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(1 + 4).put(result.value).putInt(itemId).array();
    }
}
