package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import lombok.AccessLevel;
import lombok.AllArgsConstructor;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record CloseBoxBody(
        CloseResult result
) implements Body {
    @AllArgsConstructor(access = AccessLevel.PRIVATE)
    public enum CloseResult {
        SUCCESS(1), FAIL(0), NOT_VIEWER(2);

        private final int value;
    }

    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(4).putInt(this.result.value).array();
    }
}
