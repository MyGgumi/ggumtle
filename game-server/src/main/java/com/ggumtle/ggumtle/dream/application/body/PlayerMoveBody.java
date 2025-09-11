package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record PlayerMoveBody(
        long id,
        int x,
        int y,
        int z
) implements Body {
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
