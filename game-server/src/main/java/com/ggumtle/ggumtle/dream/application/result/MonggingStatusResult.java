package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record MonggingStatusResult(
        long playerId,
        MonggingStatus status
) implements Result {

    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum MonggingStatus {
        ESCAPE(10)
        ;

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(12).putLong(playerId).putInt(status.value).array();
    }
}
