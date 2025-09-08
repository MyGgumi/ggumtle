package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record FeedDoneResult(
        int id
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(4).putInt(id).array();
    }
}
