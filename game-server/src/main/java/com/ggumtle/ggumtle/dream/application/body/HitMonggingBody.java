package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record HitMonggingBody(
        Result result,
        long targetMonggingId,
        int leftHp
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS(1), FAIL(0),
        NOT_PLAYER(2), NOT_MONGDUNG(3), NOT_FOUND_TARGET(4), NOT_MONGGING(5);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer
                .allocate(16)
                .putInt(result.value)
                .putLong(targetMonggingId)
                .putInt(leftHp)
                .array();
    }
}
