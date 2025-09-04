package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record PlayerMoveResult(
        long id,
        int x,
        int y,
        int z
) implements Result {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(8 + 4 * 3);

        buffer.putLong(id);
        buffer.putInt(x);
        buffer.putInt(y);
        buffer.putInt(z);

        return buffer.array();
    }
}
