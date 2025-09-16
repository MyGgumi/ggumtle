package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record DigUpReceiveBody(
        Result result
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        FAIL((byte) 0), START_DIGGING((byte) 1), NOT_FOUND_GGUMTLE((byte) 2), NOT_FOUND_MONGGING((byte) 3),
        ALREADY_DIG_UP((byte) 10), NOT_AROUND((byte) 11);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{this.result.value};
    }
}
