package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record DigUpResult(
        int id,
        boolean isRealGgumtle
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + 1);

        buffer.putInt(id);
        buffer.put(isRealGgumtle ? (byte) 1 : (byte) 0);

        return buffer.array();
    }
}
