package com.ggumtle.ggumtle.dream.application.body;

import com.ggumtle.ggumtle.common.dto.Body;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record NewGgumtleBody(
        int id,
        Position position
) implements Body {
    @Override
    public byte[] toBytes(Charset charsets) {
        ByteBuffer buffer = ByteBuffer.allocate(4 + 4 * 3);

        buffer.putInt(id);
        buffer.putInt(position.x);
        buffer.putInt(position.y);
        buffer.putInt(position.z);

        return buffer.array();
    }
}
