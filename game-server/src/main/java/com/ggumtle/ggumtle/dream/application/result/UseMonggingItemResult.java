package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record UseMonggingItemResult(
        Status status,
        int itemId
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        FAIL((byte) 0), SUCCESS((byte) 1),
        NOT_FOUND_ITEM_ID((byte) 2),
        NOT_MONGGING((byte) 10), NOT_ATTACK_ITEM((byte) 11), NOT_FOUND_ITEM((byte) 12), MISS((byte) 13);
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(1 + 4).put(status.value).putInt(itemId).array();
    }
}
