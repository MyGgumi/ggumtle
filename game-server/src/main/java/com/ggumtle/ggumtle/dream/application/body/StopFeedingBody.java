package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record StopFeedingBody(
    Result result,
    int leftFeedItemCount
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Result {
        FAIL((byte) 1), STOP((byte) 1), NOT_FOUND((byte) 1);

        final byte value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(1 + 4).put(result.value).putInt(leftFeedItemCount).array();
    }
}
