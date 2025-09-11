package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record DigUpBody(
        int id,
        boolean isRealGgumtle
) implements Body {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + 1);

        buffer.putInt(id);
        buffer.put(isRealGgumtle ? (byte) 1 : (byte) 0);

        return buffer.array();
    }
}
