package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.common.dto.Result;
import com.ggumtle.ggumtle.dream.vo.Position;

import java.nio.ByteBuffer;
import java.nio.charset.Charset;

public record NewGgumtleResult(
        int id,
        Position position
) implements Result {
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
