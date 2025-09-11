package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;

import java.nio.charset.Charset;

public record DoneReviveBody(
        long revivedMonggingId
) implements Body {
    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[0];
    }
}
