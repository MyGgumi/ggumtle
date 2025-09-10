package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StartReviveResult(
        Status result
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        SUCCESS((byte) 1),
        NOT_FOUND_PLAYER((byte) 2),
        NOT_MONGGING((byte) 10), NOT_KNOCKOUT((byte) 11),
        ;

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[0];
    }
}
