package com.ggumtle.ggumtle.room.application.result;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record SceneChangeResult(
        int result
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        return ByteBuffer.allocate(4).putInt(result).array();
    }
}
