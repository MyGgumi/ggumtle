package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.charset.Charset;

public record DoneReviveResult(
        long revivedMonggingId
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        return new byte[0];
    }
}
