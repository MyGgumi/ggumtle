package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record HitMonggingBody(
        Result result,
        int leftHp
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0), NOT_PLAYER(2), NOT_MONGDUNG(3), NOT_FOUND_TARGET(4);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer
                .allocate(8)
                .putInt(result.value)
                .putInt(leftHp)
                .array();
    }
}
