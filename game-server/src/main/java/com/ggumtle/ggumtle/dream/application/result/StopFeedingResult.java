package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record StopFeedingResult(
    Status result,
    int leftFeedItemCount
) implements Result {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        FAIL((byte) 1), STOP((byte) 1), NOT_FOUND((byte) 1);

        final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(1 + 4).put(result.value).putInt(leftFeedItemCount).array();
    }
}
