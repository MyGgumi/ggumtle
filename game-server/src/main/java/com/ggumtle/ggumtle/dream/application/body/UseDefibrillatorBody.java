package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record UseDefibrillatorBody(
        Result result,
        int hp
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        FAIL((byte) 0), SUCCESS((byte) 1),
        NOT_FOUND_MONGGING((byte) 2), NOT_KNOCK_OUT((byte) 3), NOT_FOUND_DEFIBRILLATOR((byte) 4);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(5).put(result.value).putInt(hp).array();
    }
}
