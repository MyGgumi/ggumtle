package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record MonggingStatusBody(
        long playerId,
        Result result
) implements Body {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        NORMAL(1),
        KNOCKOUT(50),
        DEAD(70),
        ESCAPE(100),
        STUNNED(120),
        ;

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(12).putLong(playerId).putInt(result.value).array();
    }
}
