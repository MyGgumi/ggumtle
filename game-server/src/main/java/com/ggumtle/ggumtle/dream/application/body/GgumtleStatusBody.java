package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record GgumtleStatusBody(
        int id,
        Status status
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum Status {
        BURY(1), NORMAL(10), DIGGING(2), FEEDING(20), DONE(30), FAKE(11);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(8);

        buffer.putInt(id);
        buffer.putInt(status.value);

        return buffer.array();
    }
}
