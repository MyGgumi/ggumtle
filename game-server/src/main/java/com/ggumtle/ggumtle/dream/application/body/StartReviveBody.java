package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StartReviveBody(
        Result result
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS((byte) 1),
        NOT_FOUND_MONGGING((byte) 2),
        NOT_KNOCKOUT((byte) 10), NOT_NEAR((byte) 11),
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{ this.result.value };
    }
}
