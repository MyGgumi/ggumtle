package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record DoneReviveBody(
        long revivedMonggingId
) implements Body {
    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(8).putLong(revivedMonggingId).array();
    }
}
