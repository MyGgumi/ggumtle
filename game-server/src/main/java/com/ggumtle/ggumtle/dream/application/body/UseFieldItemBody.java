package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record UseFieldItemBody(
        Result result,
        int fieldItemId
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS((byte) 1),
        NOT_FOUND_FIELD_ITEM((byte) 2),
        NOT_MONGGING((byte) 10), ALREADY_USED((byte) 11), NOT_NEAR((byte) 12);
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(1 + 4).put(result.value).putInt(fieldItemId).array();
    }
}
