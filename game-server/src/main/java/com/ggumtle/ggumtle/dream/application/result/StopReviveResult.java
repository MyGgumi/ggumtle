package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StopReviveResult(
        Status status
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        FAIL((byte) 0), SUCCESS((byte) 1);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{ status.value };
    }
}
