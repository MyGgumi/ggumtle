package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record EscapeBody(
        Result result
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        SUCCESS((byte) 1), FAIL((byte) 0),
        NOT_FOUND_EXIT((byte) 2), NOT_MONGGING((byte) 3),
        NOT_IN_EXIT((byte) 10), NOT_ALIVE((byte) 11)
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{ result.value };
    }
}
