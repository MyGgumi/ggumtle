package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.charset.Charset;

public record StopDiggingResult(
        Status result
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        STOP((byte) 1), NOT_FOUND_DIGGING((byte) 2);

        private final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[]{this.result.value};
    }
}
